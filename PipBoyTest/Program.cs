using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using PipBoy;
using PipBoy.Debugging;
using PipBoyDump;

namespace PipBoyTest
{
    class Program
    {
        static void MainSimple(string[] args)
        {
            using (var streamProvider = new PipBoyStreamProvider())
            using (var stream = streamProvider.ReadFile("data.dump"))
            {
                var gameStateReader = new GameStateReader(stream);
                while (gameStateReader.NextState())
                {
                    Console.WriteLine("Player X position: " + (float)gameStateReader.GameState.Map.World.Player.X);
                }
            }
        }

        static void Main(string[] args)
        {
            using (var streamProvider = new PipBoyStreamProvider(DebugSettings.DumpTcpStream ? DebugSettings.TcpDumpFile : null))
            {
                var stream = DebugSettings.UseTcp
                    ? streamProvider.Connect(DebugSettings.Host, DebugSettings.Port)
                    : streamProvider.ReadFile(DebugSettings.InputFile);

                using (stream)
                {
                    ReadStream(stream);
                }
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("finished");
                Console.ReadLine();
            }
        }

        private static void ReadStream(Stream stream)
        {
            //var gameStateReader = new GameStateReader(stream, new GameStateReaderDebugSettings { Writer = Console.Out, DumpInitialPacketParsing = true, DumpPacketParsing = true });
            var gameStateReader = new GameStateReader(stream);

            gameStateReader.NextState();        // read first state
            var playerPosition = (GameObject)gameStateReader.GameState.Map.World.Player;
            playerPosition.Changed += PlayerPosition_Changed;

            while (!Console.KeyAvailable && gameStateReader.NextState())
            {
                // process events
            }
        }

        private static void PlayerPosition_Changed(object sender, GameObjectChangedEvent e)
        {
            foreach (var changedChild in e.ChangedChildren)
            {
                Console.WriteLine("position changed: " + changedChild.ToString(true));
            }
        }
    }
}
