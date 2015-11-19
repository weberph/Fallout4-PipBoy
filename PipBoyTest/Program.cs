using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using PipBoy;

namespace PipBoyTest
{
    // uses Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform

    class Program
    {
        private static readonly ManualResetEvent ExitMutex = new ManualResetEvent(false);
        
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
            // read header
            var reader = new BinaryReader(stream);
            var headerMetaPacket = reader.ReadBytes(5);
            var headerPacket = reader.ReadBytes(headerMetaPacket[0]); // ~35 bytes

            if (keepAlive)
            {
                var sendThread = new Thread(SendThread);
                sendThread.IsBackground = true;
                sendThread.Start(stream);
            }

            var gameStateReader = new GameStateReader(stream);

            while (!Console.KeyAvailable && gameStateReader.NextState())
            {
                Console.WriteLine("Player X position: " + (float)gameStateReader.GameState.Map.World.Player.X);
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
