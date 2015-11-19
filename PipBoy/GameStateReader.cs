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

        public GameStateReaderDebugSettings()
        {
        }
    }

    public class GameStateReader
    {
        private readonly GameStateReaderDebugSettings _debugSettings;

        private readonly BinaryReader _reader;
        private readonly PacketParser _packetParser;
        private readonly Codebook _codebook;
        private GameStateManager _gameStateManager;
        private bool _first = true;

        public dynamic GameState => _gameStateManager.GameState;

        public GameStateReader(Stream stream, GameStateReaderDebugSettings debugSettings = null)
        {
            _reader = new BinaryReader(stream);
            _packetParser = new PacketParser();
            _codebook = new Codebook();
            _debugSettings = debugSettings ?? new GameStateReaderDebugSettings();
        }

        public bool NextState()
        {
            int size;
            do
            {
                var metaPacket = _reader.ReadBytes(5);
                if (metaPacket.Length < 5)
                {
                    return false;
                }
                size = BitConverter.ToInt32(metaPacket, 0);
            } while (size == 0);

            var dataPacket = _reader.ReadBytes(size);
            var data = _packetParser.Process(dataPacket);

            if (_first)
            {
                _gameStateManager = new GameStateManager(data);
            }
            else
            {
                _gameStateManager.Update(data);
            }

            if (_debugSettings.IsLoggingDisabled)
            {
                _codebook.Append(data);
                DebugDump(dataPacket, data);
            }
            _first = false;
            return true;
        }

        private void DebugDump(byte[] dataPacket, Dictionary<uint, DataElement> data)
        {
            if ((_first && _debugSettings.DumpInitialPacketParsing) || (!_first && _debugSettings.DumpPacketParsing))
            {
                new PacketParser(_codebook, Console.Out).Process(dataPacket);
                if (_first && _debugSettings.DumpInitialPacketContent)
                {
                    InitialPacketDumper.DumpInitialPacket(data, _debugSettings.Writer);
                }
                Console.WriteLine("==================================================");
            }
        }
    }
}
