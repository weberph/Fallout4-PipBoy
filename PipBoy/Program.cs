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
        static ManualResetEvent _exitMutex = new ManualResetEvent(false);

        static byte[] PauseCommand = "00 0f 00 00 00 01".ToByteArray();
        static byte[] UnpauseCommand = "00 0f 00 00 00 00".ToByteArray();

        enum SimpleCommand
        {
            Unknown,
            Pause,
            Unpause,
            Direction,
            Movement,
            Time
        }

        static Dictionary<byte[], SimpleCommand> CommandMap = new Dictionary<byte[], SimpleCommand>
            {
                { PauseCommand, SimpleCommand.Pause },
                { UnpauseCommand, SimpleCommand.Unpause },
            };

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

        private static void ProcessInitialPacket(byte[] metaPacket, byte[] dataPacket)
        {
            var data = ParsePacket(dataPacket);

            _codebook = BuildCodebook(data);
            var name = _codebook[36514];

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

        static void ProcessPacket(byte[] metaPacket, byte[] dataPacket)
        {
            //var command = GetPacketCommand(dataPacket);
            var parsedPacket = ParsePacket(dataPacket);

            //DumpPacket(dataPacket);

            //if (command == SimpleCommand.Unknown)
            //{
            //    Console.Write("? ");
            //    DumpPacket(dataPacket);
            //}
            //else if (command == SimpleCommand.Direction)
            //{
            //    PrintDirectionPacket(dataPacket);
            //}
            //else if (command == SimpleCommand.Time)
            //{
            //    PrintTimePacket(dataPacket);
            //}
            //else if (command == SimpleCommand.Movement)
            //{
            //    PrintMovementPacket(dataPacket);
            //}
            //else
            //{
            //    Console.WriteLine(command.ToString());
            //}
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

        //private static Dictionary<string, List<Dictionary<string, DataElement>>> ReadMapOfAttributeMaps(Dictionary<uint, DataElement> data, uint itemIndex)
        //{
        //    // reads index -> map<AttributeMapIndex, CategoryName> -> map<AttributeName, AttributeValueIndex>
        //    var result = new Dictionary<string, List<Dictionary<string, DataElement>>>();
        //    var categories = ((MapElement)data[itemIndex]).Value;
        //    foreach (var category in categories)
        //    {
        //        result.Add(category.Value, ReadListOfAttributeMaps(data, category.Key));
        //    }
        //    return result;
        //}

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

        //private static void PrintAttributeCategorizedMaps(Dictionary<string, List<Dictionary<string, DataElement>>> categorizedMap)
        //{
        //    foreach (var attributeMaps in categorizedMap)
        //    {
        //        Console.WriteLine(attributeMaps.Key);
        //        Console.WriteLine(new string('=', attributeMaps.Key.Length));
        //        Console.WriteLine();

        //        PrintAttributeMaps(attributeMaps.Value);
        //    }
        //}

        private static Dictionary<uint, DataElement> ParsePacket(byte[] dataPacket)
        {
            var result = new Dictionary<uint, DataElement>();
            var reader = new BinaryReader(new MemoryStream(dataPacket), Encoding.ASCII);
            while (reader.PeekChar() != -1)
            {
                var type = reader.ReadByte();
                var id = reader.ReadUInt32();
                Console.Write("[{0}", id);
                if(_codebook != null)
                {
                    string name = "<unknown>";
                    _codebook.TryGetValue(id, out name);
                    Console.Write(" - {0}", name);
                }
                Console.Write("] ");
                switch (type)
                {
                    case 0x00:
                        bool boolean = (reader.ReadByte() != 0);
                        Console.WriteLine("bool: " + boolean);
                        result.Add(id, new BoolElement(boolean));
                        break;
                    case 0x01:  // int8 and uint8 might be switched
                        byte uint8 = reader.ReadByte();
                        Console.WriteLine("uint8: " + uint8);
                        result.Add(id, new UInt8Element(uint8));
                        break;
                    case 0x02:
                        sbyte int8 = reader.ReadSByte();
                        Console.WriteLine("sint8: " + int8);
                        result.Add(id, new Int8Element(int8));
                        break;
                    case 0x03:
                        var int32 = reader.ReadInt32();
                        Console.WriteLine("sint32: " + int32);
                        result.Add(id, new Int32Element(int32));
                        break;
                    case 0x04:
                        var uint32 = reader.ReadUInt32();
                        Console.WriteLine("uint32: " + uint32);
                        result.Add(id, new UInt32Element(uint32));
                        break;
                    case 0x05:
                        var single = reader.ReadSingle();
                        Console.WriteLine("float: " + single);
                        result.Add(id, new FloatElement(single));
                        break;
                    case 0x06:
                        var str = reader.ReadCString();
                        Console.WriteLine("String: " + str);
                        result.Add(id, new StringElement(str));
                        break;
                    case 0x07:
                        var listCount = reader.ReadUInt16();
                        var list = new List<UInt32>();
                        Console.WriteLine("list of " + listCount);
                        for (var i = 0; i < listCount; i++)
                        {
                            var listValue = reader.ReadUInt32();
                            list.Add(listValue);
                            Console.WriteLine("\t" + listValue);
                        }
                        result.Add(id, new ListElement(list));
                        break;
                    case 0x08:
                        var mapCount = reader.ReadUInt16();
                        var map = new Dictionary<UInt32, string>();
                        Console.WriteLine("map of " + mapCount);
                        for (var i = 0; i < mapCount; i++)
                        {
                            var mapId = reader.ReadUInt32();
                            var mapValue = reader.ReadCString();
                            map.Add(mapId, mapValue);
                            Console.WriteLine("\t" + mapId + " = " + mapValue);
                        }
                        var zeroTerminator = reader.ReadBytes(2);
                        Debug.Assert(zeroTerminator[0] == 0 && zeroTerminator[1] == 0);
                        result.Add(id, new MapElement(map));
                        break;
                    default:
                        Debugger.Break();
                        break;
                }
            }
            return result;
        }

        class Position
        {
            public float X;
            public float Y;
        }

        static Position _lastPosition;
        private static Dictionary<uint, string> _codebook;

        private static void PrintMovementPacket(byte[] dataPacket)
        {
            if (dataPacket.Length % 9 != 0)
            {
                Console.WriteLine("unknown movement packet");
                DumpPacket(dataPacket);
                return;
            }

            Console.Write("Position: ");
            //for (int i = 0; i < dataPacket.Length; i += 9)
            //{
            //    var value = BitConverter.ToSingle(dataPacket, i + 5);
            //    Console.Write("[{0:X2}] {1}, ", dataPacket[i + 1], value);
            //}

            var p = new Position();

            for (int i = 0; i < dataPacket.Length; i += 9)
            {
                var tag = dataPacket[i + 1];
                if (tag == 0x94 || tag == 0x98)
                {
                    p.X = BitConverter.ToSingle(dataPacket, i + 5);
                }
                else if (tag == 0x95 || tag == 0x99)
                {
                    p.Y = BitConverter.ToSingle(dataPacket, i + 5);
                }
            }

            if (p.X == 0f || p.Y == 0f)
            {
                if (_lastPosition != null)
                {
                    if (p.X == 0f)
                    {
                        p.X = _lastPosition.X;
                    }

                    if (p.Y == 0f)
                    {
                        p.Y = _lastPosition.Y;
                    }
                }
            }
            _lastPosition = p;
            Console.WriteLine("X: {0}, Y: {1}", p.X, p.Y);
        }

        private static void DumpPacket(byte[] dataPacket)
        {
            Console.WriteLine("[{0}]: {1}", dataPacket.Length, dataPacket.ToHexString());
        }

        private static void PrintTimePacket(byte[] dataPacket)
        {
            if (dataPacket.Length != 9)
            {
                Console.WriteLine("unknown time packet: ");
                DumpPacket(dataPacket);
                return;
            }

            var value = BitConverter.ToSingle(dataPacket, 5);
            var hours = (int)value;
            var minutes = (int)(60 * value - 60 * hours);
            Console.WriteLine("Time: {0}:{1:D2}", hours, minutes);
        }

        private static void PrintDirectionPacket(byte[] dataPacket)
        {
            if (dataPacket.Length % 9 != 0)
            {
                Console.WriteLine("unknown direction packet");
                DumpPacket(dataPacket);
                return;
            }

            Console.Write("Angle: ");
            for (int i = 0; i < dataPacket.Length; i += 9)
            {
                var value = BitConverter.ToSingle(dataPacket, i + 5);
                Console.Write(value);
            }
            Console.WriteLine();
        }

        static SimpleCommand GetPacketCommand(byte[] packet)
        {
            SimpleCommand command;
            if (CommandMap.TryGetValue(packet, out command))
            {
                return command;
            }

            if (packet.Length < 2)
            {
                return SimpleCommand.Unknown;
            }

            if (packet[1] == 0x9a)
            {
                return SimpleCommand.Direction;
            }

            if (packet[1] == 0x94 || packet[1] == 0x95 || packet[1] == 0x98 || packet[1] == 0x99)
            {
                return SimpleCommand.Movement;
            }

            if (packet[0] == 0x05 && packet[1] == 0x52 && packet[2] == 0x7a)    // packet[1] == 0x52 is not unique
            {
                return SimpleCommand.Time;
            }

            return SimpleCommand.Unknown;
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
        public static byte[] ToByteArray(this string hexString)
        {
            return hexString.Split(' ').Select(b => byte.Parse(b, System.Globalization.NumberStyles.HexNumber)).ToArray();
        }

        public static string ToHexString(this byte[] data, string delimiter = " ")
        {
            return data.Aggregate(new StringBuilder(), (a, b) => a.Append(delimiter).Append(b.ToString("X2"))).ToString().Substring(delimiter.Length);
        }

        public static bool MatchHex(this byte[] data, string hexString)
        {
            return data.SequenceEqual(hexString.ToByteArray());
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
