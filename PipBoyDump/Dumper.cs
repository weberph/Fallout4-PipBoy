using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipBoy;

namespace PipBoyDump
{

    class Dumper
    {
        private readonly string _objectFile;

        public Dumper(string objectFile = null)
        {
            _objectFile = objectFile;
        }

        public void Dump(Stream stream)
        {
            if (_objectFile != null)
            {
                using (var writer = new StreamWriter(new FileStream(_objectFile, FileMode.Create)))
                {
                    try
                    {
                        var gameStateReader = new GameStateReader(stream);
                        gameStateReader.NextState(); // read first state
                        var state = (GameObject)gameStateReader.GameState;
                        state.Changed += (s, e) =>
                        {
                            foreach (var changedChild in e.ChangedChildren)
                            {
                                writer.WriteLine(changedChild.ToString(true));
                            }
                            writer.WriteLine("===============================================================================");
                        };

                        while (gameStateReader.NextState())
                        {
                            // do nothing
                        }

                        return;
                    }
                    catch (Exception e)
                    {
                        writer.WriteLine("Exception reading game objects: " + e.Message);
                        writer.WriteLine("Stream position: " + stream.Position);
                        writer.WriteLine(e.StackTrace);
                    }
                }
            }

            // if no objectFile or an exception occured
            ReadToEnd(stream);
        }

        private static void ReadToEnd(Stream stream)
        {
            var buffer = new byte[1024];
            int read;
            do
            {
                read = stream.Read(buffer, 0, buffer.Length);
            } while (read != 0);
        }

        public static void Run(Options options)
        {
            using (var streamProvider = new PipBoyStreamProvider(options.RawFile))
            {
                Stream stream;

                if (options.Host != null)
                {
                    stream = streamProvider.Connect(options.Host, options.Port);
                }
                else if (options.InputFile != null)
                {
                    stream = streamProvider.ReadFile(options.InputFile);
                }
                else
                {
                    throw new ArgumentException();
                }

                using (stream)
                {
                    var dumper = new Dumper(options.GameobjectsFile);
                    dumper.Dump(stream);
                }
            }
        }
    }
}
