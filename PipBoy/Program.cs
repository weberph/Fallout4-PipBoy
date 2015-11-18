using System;
using System.Collections.Generic;
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
        private static Dictionary<uint, string> _codebook;
        private static PacketParser _packetParser;

        static void Main(string[] args)
        {
            if (DebugSettings.UseTcp)
            {
                using (var tcpClient = new TcpClient())
                {
                    tcpClient.Connect(DebugSettings.Host, DebugSettings.Port);
                    var stream = tcpClient.GetStream();
                    ReadStream(stream, true);
                }
            }
            else
            {
                var initialPacket = File.ReadAllBytes("Mess0.bin");
                var sizePacket = BitConverter.GetBytes(initialPacket.Length);
                var data = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }  // headerMetaPacket
                    .Concat(new byte[] { 0x00 })                        // headerPacket
                    .Concat(sizePacket).Concat(new byte[1])             // metaPacket of initialPacket
                    .Concat(initialPacket).ToArray();

                using (var ms = new MemoryStream(data))
                {
                    ReadStream(ms, false);
                }
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

            // read data
            bool first = true;
            while (!Console.KeyAvailable)
            {
                var metaPacket = reader.ReadBytes(5);
                if (metaPacket.Length < 5)
                {
                    break;
                }

                if (metaPacket[0] != 0)
                {
                    var size = BitConverter.ToInt32(metaPacket, 0);
                    var dataPacket = reader.ReadBytes(size);
                    if (first)
                    {
                        ProcessInitialPacket(metaPacket, dataPacket);
                        first = false;
                    }
                    else
                    {
                        ProcessPacket(metaPacket, dataPacket);
                    }
                }
            }
            ExitMutex.Set();
        }

        private static void ProcessInitialPacket(byte[] metaPacket, byte[] dataPacket)
        {
            var data = new PacketParser().Process(dataPacket);
            _codebook = BuildCodebook(data);

            if (DebugSettings.DumpInitialPacketParsing)
            {
                new PacketParser(_codebook, Console.Out).Process(dataPacket);
            }

            _packetParser = new PacketParser(_codebook, DebugSettings.DumpPacketParsing ? Console.Out : null);

            if (DebugSettings.DumpInitialPacketContent)
            {
                DumpInitialPacket(data);
            }
        }

        static void ProcessPacket(byte[] metaPacket, byte[] dataPacket)
        {
            _packetParser.Process(dataPacket);
        }

        private static Dictionary<uint, string> BuildCodebook(Dictionary<uint, DataElement> data)
        {
            var codebook = new Dictionary<uint, string>();
            BuildCodebook(data, 0, "", codebook);
            return codebook;
        }

        private static void BuildCodebook(Dictionary<uint, DataElement> data, uint index, string prefix, Dictionary<uint, string> codebook)
        {
            var map = ((MapElement)data[index]).Value;

            foreach (var item in map)
            {
                var name = item.Value;
                if (prefix.Length > 0)
                {
                    name = prefix + "::" + name;
                }

                codebook.Add(item.Key, name);

                var element = data[item.Key];
                if (element is MapElement)
                {
                    BuildCodebook(data, item.Key, name, codebook);
                }
                else if (element is ListElement)
                {
                    // note: no nested lists supported
                    var list = ((ListElement)element).Value;
                    if (list.Count > 0 && data[list[0]] is MapElement)
                    {
                        foreach (var subElement in list)
                        {
                            BuildCodebook(data, subElement, name, codebook);
                        }
                    }
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
            var stream = (NetworkStream)streamObj;

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
