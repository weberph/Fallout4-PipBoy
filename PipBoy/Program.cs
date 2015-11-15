using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipBoy
{
    // uses Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform

    class Program
    {
        private static ManualResetEvent _exitMutex = new ManualResetEvent(false);
        private static Dictionary<uint, string> _codebook;

        private static bool _dumpInitialPacketParsing = true;
        private static bool _dumpInitialPacketContent = true;
        private static bool _dumpPacketParsing = true;
        private static PacketParser _packetParser;

        static void Main(string[] args)
        {
            using (var tcpClient = new TcpClient())
            {
                tcpClient.Connect("192.168.0.133", 27000);
                Console.WriteLine("connected");
                var stream = tcpClient.GetStream();
                var reader = new BinaryReader(stream);

                // read header
                var headerMetaPacket = reader.ReadBytes(5);
                var headerPacket = reader.ReadBytes(headerMetaPacket[0]);   // ~35 bytes

                // start keepalive
                var sendThread = new Thread(SendThread);
                sendThread.IsBackground = true;
                sendThread.Start(stream);

                // read data
                bool first = true;
                while (!Console.KeyAvailable)
                {
                    var metaPacket = reader.ReadBytes(5);

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
                _exitMutex.Set();
            }
        }

        private static void ProcessInitialPacket(byte[] metaPacket, byte[] dataPacket)
        {
            var data = new PacketParser(_dumpInitialPacketParsing ? Console.Out : null).Process(dataPacket);

            _codebook = BuildCodebook(data);
            _packetParser = new PacketParser(_codebook, _dumpPacketParsing ? Console.Out : null);

            if (_dumpInitialPacketContent)
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
            BuildCodebook(data, 0, "", ref codebook);
            return codebook;
        }

        private static void BuildCodebook(Dictionary<uint, DataElement> data, uint index, string prefix, ref Dictionary<uint, string> codebook)
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
                    BuildCodebook(data, item.Key, name, ref codebook);
                }
                else if (element is ListElement)
                {
                    // note: no nested lists supported
                    var list = ((ListElement)element).Value;
                    if (list.Count > 0 && data[list[0]] is MapElement)
                    {
                        foreach (var subElement in list)
                        {
                            BuildCodebook(data, subElement, name, ref codebook);
                        }
                    }
                }
            }
        }

        private static void DumpInitialPacket(Dictionary<uint, DataElement> data)
        {
            var objectsByCategory = new Dictionary<string, List<Dictionary<string, DataElement>>>();

            var categories = ((MapElement)data[0]).Value;

            var radioIndex = categories.First(c => c.Value == "Radio").Key;
            var radioStations = ReadListOfAttributeMaps(data, radioIndex);
            PrintAttributeMaps(radioStations);

            var perksIndex = categories.First(c => c.Value == "Perks").Key;
            var perks = ReadListOfAttributeMaps(data, perksIndex);
            PrintAttributeMaps(perks);

            var statsIndex = categories.First(c => c.Value == "Stats").Key;
            var stats = ReadListOfAttributes(data, statsIndex);
            PrintAttributeMap(stats);

            var specialIndex = categories.First(c => c.Value == "Special").Key;
            var special = ReadListOfAttributeMaps(data, specialIndex);
            PrintAttributeMaps(special);

            // Inventory
            var inventoryIndex = categories.First(c => c.Value == "Inventory").Key;

            var inventory = ((MapElement)data[inventoryIndex]).Value;
            // 47 -> list<attributeMapIndex> (e.g. Federal Ration Stockpile Password)
            // HolotapePlaying -> bool
            // 44 -> list<attributeMapIndex> (e.g. .38 Round)
            // UnderwearType -> int32 (e.g. 1)
            // stimpakObjectIDIsValid -> bool
            // InvComponents -> list<attributeMapIndex> (e.g. Silver)
            // 43 -> list<attributeMapIndex> (e.g. Nuka Grenade)
            // 30 -> list<attributeMapIndex> (e.g. Grognak the Barbarian)
            // stimpakObjectID -> sint (e.g. 24601)
            // radawayObjectID -> sint (e.g. 25442)
            // 50 -> list<attributeMapIndex> (e.g. Atomic Command)
            // 48 -> list<attributeMapIndex> (e.g. Stimpack)
            // 35 -> list<attributeMapIndex> (e.g. Bobby Pin)
            // SortMode -> uint32 (e.g. 0)
            // Version -> sint32 (e.g. 16)
            // stimpakObjectIDIsValid -> bool
            // 29 -> list<attributeMapIndex> (e.g. Wedding Ring)
            // sortedIDS -> list<Index> -> sint32 -> attributeMap (e.g. .308 Round)

            // 48 = aid
            var aid = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "48").Key);
            PrintAttributeMaps(aid);

            // 50 = misc: recordings/holotapes
            var tapes = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "50").Key);
            PrintAttributeMaps(tapes);

            // 43 = aid
            var weapons = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "43").Key);
            PrintAttributeMaps(weapons);

            // 30 = misc: books/letters
            var books = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "30").Key);
            PrintAttributeMaps(books);

            // 35 = junk
            var junk = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "35").Key);
            PrintAttributeMaps(junk);

            // 29 = apparel
            var apparel = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "29").Key);
            PrintAttributeMaps(apparel);

            // 47 = misc: keys/passwords
            var keys = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "47").Key);
            PrintAttributeMaps(keys);

            // 44 = ammo
            var ammo = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "44").Key);
            PrintAttributeMaps(ammo);

            // components
            var components = ReadListOfAttributeMaps(data, inventory.First(i => i.Value == "InvComponents").Key);
            PrintAttributeMaps(components);

            // --

            var quests = ReadListOfAttributeMaps(data, categories.First(c => c.Value == "Quests").Key);
            PrintAttributeMaps(quests);

            var workshops = ReadListOfAttributeMaps(data, categories.First(c => c.Value == "Workshop").Key);
            PrintAttributeMaps(workshops);

            var log = ReadListOfAttributeMaps(data, categories.First(c => c.Value == "Log").Key);
            PrintAttributeMaps(log);

            var map = ReadListOfAttributes(data, categories.First(c => c.Value == "Map").Key);
            PrintAttributeMap(map);

            var playerInfo = ReadListOfAttributes(data, categories.First(c => c.Value == "PlayerInfo").Key);
            PrintAttributeMap(playerInfo);

            var status = ReadListOfAttributes(data, categories.First(c => c.Value == "Status").Key);
            PrintAttributeMap(status);
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
                if (_exitMutex.WaitOne(1000))
                {
                    return;
                }
            }
        }
    }

    public static class Extensions
    {
        public static string ToHexString(this byte[] data, string delimiter = " ")
        {
            return data.Aggregate(new StringBuilder(), (a, b) => a.Append(delimiter).Append(b.ToString("X2"))).ToString().Substring(delimiter.Length);
        }

        public static string ReadCString(this BinaryReader reader)
        {
            var sb = new StringBuilder();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                sb.Append((char)b);
            }
            return sb.ToString();
        }
    }
}
