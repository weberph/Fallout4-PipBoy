using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipBoy.Debugging;

namespace PipBoy
{
    public class GameStateReaderDebugSettings
    {
        public TextWriter Writer { get; set; }
        public bool DumpInitialPacketParsing { get; set; } = false;
        public bool DumpInitialPacketContent { get; set; } = false;
        public bool DumpPacketParsing { get; set; } = false;

        public bool IsLoggingDisabled => Writer == null || !(DumpInitialPacketParsing || DumpInitialPacketContent || DumpPacketParsing);
    }

    public class GameStateReader
    {
        private readonly GameStateReaderDebugSettings _debugSettings;

        private readonly BinaryReader _reader;
        private readonly PacketParser _packetParser;
        private readonly Codebook _codebook;
        private bool _first = true;

        public dynamic GameState => GameStateManager.GameState;
        public GameStateManager GameStateManager { get; private set; }

        public event EventHandler<LocalMapEventArgs> LocalMapUpdate;

        public GameStateReader(Stream stream, GameStateReaderDebugSettings debugSettings = null)
        {
            _reader = new BinaryReader(stream, Encoding.UTF8, true);
            _packetParser = new PacketParser();
            _codebook = new Codebook();
            _debugSettings = debugSettings ?? new GameStateReaderDebugSettings();
        }

        public bool NextState()
        {
            int size;
            byte packetType;
            do
            {
                var metaPacket = ReadSafe(5);
                if (metaPacket.Length < 5)
                {
                    return false;
                }
                size = BitConverter.ToInt32(metaPacket, 0);
                packetType = metaPacket[4];
            } while (size == 0);

            if (packetType == 0x03)
            {
                var dataPacket = ReadSafe(size);
                var data = _packetParser.Process(dataPacket);
                if (_first)
                {
                    GameStateManager = new GameStateManager(data);
                }
                else
                {
                    GameStateManager.Update(data);
                }

                if (!_debugSettings.IsLoggingDisabled)
                {
                    _codebook.Append(data);
                    DebugDump(dataPacket, data);
                }
                _first = false;
            }
            else if (packetType == 0x04)
            {
                ProcessLocalMapData(size);
            }

            return true;
        }

        private void ProcessLocalMapData(int totalSize)
        {
            var width = _reader.ReadInt32();
            var height = _reader.ReadInt32();
            var topLeft = new MapPoint(_reader.ReadSingle(), _reader.ReadSingle());
            var topRight = new MapPoint(_reader.ReadSingle(), _reader.ReadSingle());
            var bottomLeft = new MapPoint(_reader.ReadSingle(), _reader.ReadSingle());
            var remainingSize = totalSize - 8 * 4;
            var bitmapData = _reader.ReadBytes(remainingSize);
            var localMapData = new LocalMapData(width, height, topLeft, topRight, bottomLeft, bitmapData);
            LocalMapUpdate?.Invoke(this, new LocalMapEventArgs(localMapData));
        }

        private byte[] ReadSafe(int size)
        {
            try
            {
                return _reader.ReadBytes(size);
            }
            catch (ObjectDisposedException)
            {
                return new byte[0];
            }
        }

        private void DebugDump(byte[] dataPacket, Dictionary<uint, DataElement> data)
        {
            if ((_first && _debugSettings.DumpInitialPacketParsing) || (!_first && _debugSettings.DumpPacketParsing))
            {
                new PacketParser(_codebook, _debugSettings.Writer).Process(dataPacket);
                if (_first && _debugSettings.DumpInitialPacketContent)
                {
                    InitialPacketDumper.DumpInitialPacket(data, _debugSettings.Writer);
                }
                _debugSettings.Writer.WriteLine("==================================================");
            }
        }
    }

    public class LocalMapEventArgs : EventArgs
    {
        public LocalMapData MapData { get; }

        public LocalMapEventArgs(LocalMapData mapData)
        {
            MapData = mapData;
        }
    }
}
