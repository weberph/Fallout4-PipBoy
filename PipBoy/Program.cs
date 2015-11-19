using System;
using System.Diagnostics;
using System.IO;
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
        private static GameStateManager _gameStateManager;

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

                if (first)
                {
                    _gameStateManager = new GameStateManager(data);
                    var x = (float)_gameStateManager.GameState.Map.World.Player.X;
                }

                if ((first && DebugSettings.DumpInitialPacketParsing) || (!first && DebugSettings.DumpPacketParsing))
                {
                    new PacketParser(_codebook, Console.Out).Process(dataPacket);
                    if (first && DebugSettings.DumpInitialPacketContent)
                    {
                        InitialPacketDumper.DumpInitialPacket(data);
                    }
                    Console.WriteLine("==================================================");
                    first = false;
                }

            }
            ExitMutex.Set();
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
