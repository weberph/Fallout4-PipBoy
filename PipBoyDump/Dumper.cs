using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipBoy;

namespace PipBoyDump
{

    class Dumper
    {
        private readonly string _objectFile;

        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

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
                        writer.WriteLine(state.ToString(true));
                        state.Changed += (s, e) =>
                        {
                            foreach (var changedChild in e.ChangedChildren)
                            {
                                writer.WriteLine(changedChild.ToString(true));
                            }
                            writer.WriteLine("===============================================================================");
                        };
                        
                        while (!_cancelTokenSource.Token.IsCancellationRequested && gameStateReader.NextState())
                        {
                            // do nothing
                        }

                        return;
                    }
                    catch (Exception e)
                    {
                        writer.WriteLine("Exception reading game objects: " + e.Message);
                        writer.WriteLine(e.StackTrace);
                    }
                }
            }

            // if no objectFile or an exception occured
            ReadToEnd(stream);
        }

        public void SignalStop()
        {
            _cancelTokenSource.Cancel();
        }

        private void ReadToEnd(Stream stream)
        {
            var buffer = new byte[1024];
            int read;
            do
            {
                read = stream.Read(buffer, 0, buffer.Length);
            } while (!_cancelTokenSource.Token.IsCancellationRequested && read != 0);
        }
    }
}
