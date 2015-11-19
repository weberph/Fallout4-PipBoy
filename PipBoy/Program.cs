using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace PipBoy
{
    // uses Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform

    class Program
    {
        private static readonly ManualResetEvent ExitMutex = new ManualResetEvent(false);
        private static Codebook _codebook;
        private static PacketParser _packetParser;

        static void Main(string[] args)
        {
            if (DebugSettings.UseTcp)
            {
                using (var tcpClient = new TcpClient())
                {
                    tcpClient.Connect(DebugSettings.Host, DebugSettings.Port);
                    Stream stream = tcpClient.GetStream();
                    if (DebugSettings.DumpTcpStream)
                    {
                        stream = new CopyInputStream(stream, new FileStream(DebugSettings.TcpDumpFile, FileMode.Create), true);
                    }

                    using (stream)
                    {
                        ReadStream(stream, true);
                    }
                }
            }
            else
            {
                var data = File.ReadAllBytes(DebugSettings.InputFile);
                using (var ms = new MemoryStream(data))
                {
                    ReadStream(ms, false);
                }
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("finished");
                Console.ReadLine();
            }
        }

        private static void ReadStream(Stream stream, bool keepAlive)
        {
            var reader = new BinaryReader(stream);

            // read header
            var headerMetaPacket = reader.ReadBytes(5);
            var headerPacket = reader.ReadBytes(headerMetaPacket[0]); // ~35 bytes

            if (keepAlive)
            {
                var sendThread = new Thread(SendThread);
                sendThread.IsBackground = true;
                sendThread.Start(stream);
            }

            _packetParser = new PacketParser();
            _codebook = new Codebook();

            // read data
            bool first = true;
            while (!Console.KeyAvailable)
            {
                var metaPacket = reader.ReadBytes(5);
                if (metaPacket.Length < 5)
                {
                    break;
                }

                if (metaPacket[0] == 0)
                {
                    continue;
                }

                var size = BitConverter.ToInt32(metaPacket, 0);
                var dataPacket = reader.ReadBytes(size);

                var data = _packetParser.Process(dataPacket);
                _codebook.Append(data);

                if ((first && DebugSettings.DumpInitialPacketParsing) || (!first && DebugSettings.DumpPacketParsing))
                {
                    new PacketParser(_codebook, Console.Out).Process(dataPacket);
                    if (first && DebugSettings.DumpInitialPacketContent)
                    {
                        DumpInitialPacket(data);
                    }
                    first = false;
                }

                Console.WriteLine("==================================================");
            }
            ExitMutex.Set();
        }

        public class Codebook : Dictionary<uint, string>
        {
            private List<uint> _visitedElements;

            public void Append(Dictionary<uint, DataElement> data)
            {
                _visitedElements = new List<uint>();
                AppendInternal(data);

                while (data.Keys.Except(Keys).Any())    // while unknown keys exist: visit remaining elements
                {
                    AppendInternal(data.Keys.Except(_visitedElements).ToDictionary(k => k, k => data[k]));
                }
            }

            private void AppendInternal(Dictionary<uint, DataElement> data)
            {
                var startIndex = data.Keys.Min();
                string prefix;
                TryGetValue(startIndex, out prefix);
                BuildCodebook(data, startIndex, prefix ?? "", this);
            }

            private void BuildCodebook(Dictionary<uint, DataElement> data, uint index, string prefix, Dictionary<uint, string> codebook)
            {
                DataElement indexedElement;
                if (!data.TryGetValue(index, out indexedElement))
                {
                    return;
                }

                _visitedElements.Add(index);
                
                switch (indexedElement.Type)
                {
                    case ElementType.Map:
                        var map = ((MapElement)indexedElement).Value;
                        AddOrUpdate(codebook, index, prefix);

                        foreach (var item in map)
                        {
                            var name = item.Value;
                            if (prefix.Length > 0)
                            {
                                name = prefix + "::" + name;
                            }

                            codebook.Add(item.Key, name);
                            BuildCodebook(data, item.Key, name, codebook);
                        }
                        break;
                    case ElementType.List:
                        AddOrUpdate(codebook, index, prefix);

                        var list = ((ListElement)indexedElement).Value;
                        for (var i = 0; i < list.Count; i++)
                        {
                            var name = prefix + $"[{i}]";
                            AddOrUpdate(codebook, list[i], name);
                            BuildCodebook(data, list[i], name, codebook);
                        }
                        break;
                }
            }

            private static void AddOrUpdate(Dictionary<uint, string> codebook, uint index, string name)
            {
                string assertName;
                if (codebook.TryGetValue(index, out assertName))
                {
                    if (assertName != name)
                    {
                        if (assertName.Contains("[") && name.Contains("["))
                        {
                            if (name.Substring(0, assertName.IndexOf('[')) == assertName.Substring(0, assertName.IndexOf('[')))
                            {
                                codebook[index] = name;
                                return;
                            }
                        }
                        Debug.Assert(false);
                    }
                }
                else
                {
                    codebook.Add(index, name);
                }
            }
        }

        private static void DumpInitialPacket(Dictionary<uint, DataElement> data)
        {
            var dataMap = new DataMap(data);

            uint index;
            if (dataMap.TryGetIndex(DataCategory.Radio, out index))
            {
                var radioStations = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(radioStations);
            }

            if (dataMap.TryGetIndex(DataCategory.Perks, out index))
            {
                var perks = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(perks);
            }

            if (dataMap.TryGetIndex(DataCategory.Stats, out index))
            {
                var stats = ReadListOfAttributes(data, index);
                PrintAttributeMap(stats);
            }

            if (dataMap.TryGetIndex(DataCategory.Special, out index))
            {
                var special = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(special);
            }

            if (dataMap.TryGetIndex(DataCategory.Quests, out index))
            {
                var quests = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(quests);
            }

            if (dataMap.TryGetIndex(DataCategory.Workshop, out index))
            {
                var workshops = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(workshops);
            }

            if (dataMap.TryGetIndex(DataCategory.Log, out index))
            {
                var log = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(log);
            }

            if (dataMap.TryGetIndex(DataCategory.Map, out index))
            {
                var map = ReadListOfAttributes(data, index);
                PrintAttributeMap(map);
            }

            if (dataMap.TryGetIndex(DataCategory.PlayerInfo, out index))
            {
                var playerInfo = ReadListOfAttributes(data, index);
                PrintAttributeMap(playerInfo);
            }

            if (dataMap.TryGetIndex(DataCategory.Status, out index))
            {
                var status = ReadListOfAttributes(data, index);
                PrintAttributeMap(status);
            }

            if (dataMap.TryGetIndex(DataCategory.Inventory, out index))
            {
                var inventoryMap = new InventoryMap(data, index);

                if (inventoryMap.TryGetIndex(InventoryCategory.Aid, out index))
                {
                    var aid = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(aid);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Recordings, out index))
                {
                    var recordings = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(recordings);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Weapons, out index))
                {
                    var weapons = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(weapons);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Writings, out index))
                {
                    var books = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(books);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Junk, out index))
                {
                    var junk = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(junk);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Apparel, out index))
                {
                    var apparel = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(apparel);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Keys, out index))
                {
                    var keys = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(keys);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Ammo, out index))
                {
                    var ammo = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(ammo);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Components, out index))
                {
                    var components = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(components);
                }
            }
        }

        private static Dictionary<string, DataElement> ReadListOfAttributes(Dictionary<uint, DataElement> data, uint itemIndex)
        {
            // reads index -> map<AttributeName, AttributeValueIndex>
            var itemAttributes = new Dictionary<string, DataElement>();
            var attributes = ((MapElement)data[itemIndex]).Value;
            foreach (var attribute in attributes)
            {
                itemAttributes.Add(attribute.Value, data[attribute.Key]);
            }
            return itemAttributes;
        }

        private static List<Dictionary<string, DataElement>> ReadListOfAttributeMaps(Dictionary<uint, DataElement> data, uint itemIndex)
        {
            // reads index -> list<Index> -> map<AttributeName, AttributeValueIndex>
            var result = new List<Dictionary<string, DataElement>>();
            var itemList = ((ListElement)data[itemIndex]).Value;
            foreach (var itemAttributeListIndex in itemList)
            {
                result.Add(ReadListOfAttributes(data, itemAttributeListIndex));
            }
            return result;
        }

        private static void PrintAttributeMap(Dictionary<string, DataElement> attributeMap)
        {
            foreach (var attribute in attributeMap)
            {
                Console.WriteLine("{0}: {1}", attribute.Key, attribute.Value);
            }
            Console.WriteLine();
        }

        private static void PrintAttributeMaps(List<Dictionary<string, DataElement>> attributeMaps)
        {
            foreach (var attributeMap in attributeMaps)
            {
                PrintAttributeMap(attributeMap);
            }
        }

        static void SendThread(object streamObj)
        {
            var stream = (Stream)streamObj;

            Thread.Sleep(1000);

            while (true)
            {
                try
                {
                    var keepAlive = new byte[5];
                    stream.Write(keepAlive, 0, keepAlive.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Keepalive thread exception: " + e.Message);
                    return;
                }
                if (ExitMutex.WaitOne(1000))
                {
                    return;
                }
            }
        }
    }
}
