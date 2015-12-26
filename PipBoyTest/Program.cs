using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
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

        // ====================================================================================================

        private static readonly ManualResetEvent InitializationComplete = new ManualResetEvent(false);

        private static Dictionary<uint, string> _radioList;                                           // id => text
        private static Dictionary<uint, Tuple<string, string, uint, uint, uint, bool>> _questList;    // id => title, description, formID, type, instance, active

        static void Main(string[] args)
        {
            using (var streamProvider = new PipBoyStreamProvider(DebugSettings.DumpTcpStream ? DebugSettings.TcpDumpFile : null))
            {
                var stream = DebugSettings.UseTcp
                    ? streamProvider.Connect(DebugSettings.Host, DebugSettings.Port)
                    : streamProvider.ReadFile(DebugSettings.InputFile);

                using (stream)
                {
                    var readThread = new Thread(ReadStreamThread);
                    readThread.Start(stream);
                    ProcessUserInput(stream);

                    stream.Close();
                    readThread.Join();
                }
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("finished");
                Console.ReadLine();
            }
        }

        private static void ProcessUserInput(Stream stream)
        {
            var commandSender = new CommandSender(stream);

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;

                if (key == ConsoleKey.X)
                {
                    break;
                }

                if (!InitializationComplete.WaitOne(1000))
                {
                    Console.WriteLine("initialization not complete");
                    continue;
                }

                if (key == ConsoleKey.Q)
                {
                    Console.Write("enter quest id (empty to print all, 'a' for active quests, 'f' for finished quests): ");
                    var input = Console.ReadLine();
                    uint id;
                    Tuple<string, string, uint, uint, uint, bool> questInfo;
                    if (string.IsNullOrWhiteSpace(input) || !uint.TryParse(input, out id) || !_questList.TryGetValue(id, out questInfo))
                    {
                        bool? onlyActive = input == "a" ? true : input == "f" ? false : (bool?)null;
                        PrintQuests(onlyActive);
                        continue;
                    }
                    commandSender.Send(new Command(CommandType.ToggleQuest, questInfo.Item3, questInfo.Item5, questInfo.Item4));  // formID, instance, type
                }
                else if (key == ConsoleKey.R)
                {
                    Console.Write("enter radio station id (empty to print all): ");
                    var idInput = Console.ReadLine();
                    uint id;
                    if (string.IsNullOrWhiteSpace(idInput) || !uint.TryParse(idInput, out id))
                    {
                        PrintRadioStations();
                        continue;
                    }
                    commandSender.Send(new Command(CommandType.ToggleRadio, id));
                } else if (key == ConsoleKey.L)
                {
                    commandSender.Send(new Command(CommandType.RequestLocalMap));
                }
                else
                {
                    Console.WriteLine("r - print/toggle radio stations");
                    Console.WriteLine("q - print/toggle quests");
                    Console.WriteLine("l - request local map data");
                    Console.WriteLine("x - exit");
                }
            }
        }

        private static void PrintQuests(bool? filterByActive)
        {
            var questList = _questList;
            foreach (var quest in questList)
            {
                if (filterByActive.HasValue && quest.Value.Item6 != filterByActive.Value)
                {
                    continue;
                }

                Console.WriteLine($"\t{quest.Key} - {quest.Value.Item1}");
            }
        }

        private static void PrintRadioStations()
        {
            var radioList = _radioList;
            foreach (var station in radioList)
            {
                Console.WriteLine($"\t{station.Key} - {station.Value}");
            }
        }

        private static void ReadStreamThread(object streamObj)
        {
            var stream = (Stream)streamObj;
            //var gameStateReader = new GameStateReader(stream, new GameStateReaderDebugSettings { Writer = Console.Out, DumpInitialPacketParsing = true, DumpPacketParsing = true });
            var gameStateReader = new GameStateReader(stream);
            gameStateReader.LocalMapUpdate += GameStateReader_LocalMapUpdate;

            gameStateReader.NextState();        // read first state before starting the loop (required to register to the 'Changed' event)

            // register events
            var playerPosition = (GameObject)gameStateReader.GameState.Map.World.Player;
            playerPosition.Changed += PlayerPosition_Changed;

            var radioList = (GameObject)gameStateReader.GameState.Radio;
            radioList.Changed += RadioList_Changed;

            var questList = (GameObject)gameStateReader.GameState.Quests;
            questList.Changed += QuestList_Changed;

            _radioList = CreateRadioList(radioList);
            _questList = CreateQuestList(questList);

            InitializationComplete.Set();

            // process stream
            while (gameStateReader.NextState())
            {
                // process events
            }
        }

        private static Dictionary<uint, Tuple<string, string, uint, uint, uint, bool>> CreateQuestList(GameObject questList)
        {
            var quests = (GameObject[])(questList as dynamic);
            return quests.ToDictionary(q => q.Id, q =>
            {
                dynamic quest = q;
                return Tuple.Create((string)quest.text, (string)quest.desc, (uint)quest.formID, (uint)quest.type, (uint)quest.instance, (bool)quest.enabled);
            });
        }

        private static Dictionary<uint, string> CreateRadioList(GameObject radioList)
        {
            var stations = (GameObject[])(radioList as dynamic);
            return stations.ToDictionary(s => s.Id, s => (string)(s as dynamic).text);
        }


        // Event handlers
        
        private static void GameStateReader_LocalMapUpdate(object sender, LocalMapEventArgs e)
        {
            var bitmap = e.MapData.CreateBitmap();

            new Thread(_ =>
            {
                var form = new Form
                {
                    Width = bitmap.Width + 20,
                    Height = bitmap.Height + 50
                };

                var pictureBox = new PictureBox
                {
                    Width = bitmap.Width,
                    Height = bitmap.Height,
                    Image = bitmap
                };

                form.Controls.Add(pictureBox);
                form.Show();
                form.BringToFront();

                Application.Run(form);
            }).Start();
        }

        private static void PlayerPosition_Changed(object sender, GameObjectChangedEvent e)
        {
            foreach (var changedChild in e.ChangedChildren)
            {
                Console.WriteLine("position changed: " + changedChild.ToString(true));
            }
        }

        private static void RadioList_Changed(object sender, GameObjectChangedEvent e)
        {
            _radioList = CreateRadioList((GameObject)sender);
        }

        private static void QuestList_Changed(object sender, GameObjectChangedEvent e)
        {
            _questList = CreateQuestList((GameObject)sender);
        }
    }
}
