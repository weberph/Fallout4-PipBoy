using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using PipBoy;

namespace PipBoyTest
{
    // uses Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform

    class Program
    {
        private static readonly ManualResetEvent ExitMutex = new ManualResetEvent(false);
        private static GameStateReader _gameStateReader;

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

            _gameStateReader = new GameStateReader(stream);
            //var gameStateReader = new GameStateReader(stream, new GameStateReaderDebugSettings { Writer = Console.Out, DumpInitialPacketParsing = true, DumpPacketParsing = true });

            _gameStateReader.NextState();
            //var playerPosition = (GameObject) gameStateReader.GameState.Map.World.Player;
            //playerPosition.Changed += PlayerPosition_Changed;

            var inventory = (GameObject) _gameStateReader.GameState.Inventory;
            inventory.Changed += Inventory_Changed;

            while (!Console.KeyAvailable && _gameStateReader.NextState())
            {
                //Console.WriteLine("Player X position: " + (float)gameStateReader.GameState.Map.World.Player.X);
            }
            ExitMutex.Set();
        }

        private static void Inventory_Changed(object sender, GameObjectChangedEvent e)
        {
            foreach (var changedChild in e.ChangedChildren)
            {
                Console.WriteLine($"inventory changed: [{changedChild.Id}: {changedChild.Path}] = {changedChild}");
            }
            Console.WriteLine("================================================================================");
        }

        private static void PlayerPosition_Changed(object sender, GameObjectChangedEvent e)
        {
            foreach (var changedChild in e.ChangedChildren )
            {
                Console.WriteLine("position changed: " + changedChild);
            }
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
