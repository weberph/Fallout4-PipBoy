using System;
using System.Collections.Generic;
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
            do
            {
                var metaPacket = ReadSafe(5);
                if (metaPacket.Length < 5)
                {
                    return false;
                }
                size = BitConverter.ToInt32(metaPacket, 0);
            } while (size == 0);

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
            return true;
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
}
