﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PipBoy
{
    public class PacketParser
    {
        private readonly Dictionary<uint, string> _codebook;

        private readonly TextWriter _logger;

        public PacketParser(TextWriter logger = null)
        {
            _logger = logger ?? new StreamWriter(Stream.Null);
        }

        public PacketParser(Dictionary<uint, string> codebook, TextWriter logger = null)
            : this(logger)
        {
            _codebook = codebook;
        }

        public Dictionary<uint, DataElement> Process(byte[] dataPacket)
        {
            var result = new Dictionary<uint, DataElement>();
            var reader = new BinaryReader(new MemoryStream(dataPacket), Encoding.ASCII);
            while (reader.PeekChar() != -1)
            {
                var type = reader.ReadByte();
                var id = reader.ReadUInt32();
                _logger.Write("[{0}", id);
                if (_codebook != null)
                {
                    string name;
                    _codebook.TryGetValue(id, out name);
                    Debug.Assert(name != null);
                    _logger.Write(" - {0}", name ?? "<unknown>");
                }
                _logger.Write("] ");
                switch (type)
                {
                    case 0x00:
                        bool boolean = (reader.ReadByte() != 0);
                        _logger.WriteLine("bool: " + boolean);
                        result.Add(id, new BoolElement(boolean));
                        break;
                    case 0x01:  // int8 and uint8 might be switched
                        byte uint8 = reader.ReadByte();
                        _logger.WriteLine("uint8: " + uint8);
                        result.Add(id, new UInt8Element(uint8));
                        break;
                    case 0x02:
                        sbyte int8 = reader.ReadSByte();
                        _logger.WriteLine("sint8: " + int8);
                        result.Add(id, new Int8Element(int8));
                        break;
                    case 0x03:
                        var int32 = reader.ReadInt32();
                        _logger.WriteLine("sint32: " + int32);
                        result.Add(id, new Int32Element(int32));
                        break;
                    case 0x04:
                        var uint32 = reader.ReadUInt32();
                        _logger.WriteLine("uint32: " + uint32);
                        result.Add(id, new UInt32Element(uint32));
                        break;
                    case 0x05:
                        var single = reader.ReadSingle();
                        _logger.WriteLine("float: " + single);
                        result.Add(id, new FloatElement(single));
                        break;
                    case 0x06:
                        var str = reader.ReadCString();
                        _logger.WriteLine("String: " + str);
                        result.Add(id, new StringElement(str));
                        break;
                    case 0x07:
                        var listCount = reader.ReadUInt16();
                        var list = new List<UInt32>();
                        _logger.WriteLine("list of " + listCount);
                        for (var i = 0; i < listCount; i++)
                        {
                            var listValue = reader.ReadUInt32();
                            list.Add(listValue);
                            _logger.WriteLine("\t" + listValue);
                        }
                        result.Add(id, new ListElement(list));
                        break;
                    case 0x08:
                        var mapCount = reader.ReadUInt16();
                        var map = new Dictionary<UInt32, string>();
                        _logger.WriteLine("map of " + mapCount);
                        for (var i = 0; i < mapCount; i++)
                        {
                            var mapId = reader.ReadUInt32();
                            var mapValue = reader.ReadCString();
                            map.Add(mapId, mapValue);
                            _logger.WriteLine("\t" + mapId + " = " + mapValue);
                        }

                        // TODO: figure out if MapElement is an uint->string map with extra values or an uint->(string, value) map
                        var appendixCount = reader.ReadUInt16();
                        if (appendixCount > 0 && appendixCount != mapCount)
                        {
                            //Debugger.Break(); // TODO: investigate
                        }
                        var extraValues = new uint[appendixCount];
                        for (int i = 0; i < appendixCount; i++)
                        {
                            var someId = reader.ReadUInt32();
                            string name = null;
                            if (_codebook != null)
                            {
                                _codebook.TryGetValue(someId, out name);
                            }
                            extraValues[i] = someId;
                            _logger.WriteLine("\t\textraValue: {0}, {1}", someId, name ?? "<unknown>");
                        }

                        result.Add(id, new MapElement(map, extraValues));
                        break;
                    default:
                        Debugger.Break();
                        break;
                }
            }
            return result;
        }
    }

    public static class BinaryReaderExtensions
    {
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
