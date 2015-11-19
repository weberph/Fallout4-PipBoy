using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipBoy
{
    public class GameStateReader
    {
        private readonly BinaryReader _reader;
        private readonly PacketParser _packetParser;
        private readonly Codebook _codebook;
        private GameStateManager _gameStateManager;
        private bool _first = true;

        public dynamic GameState => _gameStateManager.GameState;


        public GameStateReader(Stream stream)
        {
            _reader = new BinaryReader(stream);
            _packetParser = new PacketParser();
            _codebook = new Codebook();
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
            _codebook.Append(data);

            if (_first)
            {
                _gameStateManager = new GameStateManager(data);
            }
            else
            {
                _gameStateManager.Update(data);
            }

            DebugDump(dataPacket, data);
            _first = false;
            return true;
        }

        private void DebugDump(byte[] dataPacket, Dictionary<uint, DataElement> data)
        {
            if ((_first && DebugSettings.DumpInitialPacketParsing) || (!_first && DebugSettings.DumpPacketParsing))
            {
                new PacketParser(_codebook, Console.Out).Process(dataPacket);
                if (_first && DebugSettings.DumpInitialPacketContent)
                {
                    InitialPacketDumper.DumpInitialPacket(data);
                }
                Console.WriteLine("==================================================");
            }
        }
    }
}
