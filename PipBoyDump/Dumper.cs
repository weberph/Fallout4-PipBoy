using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

    public class PipBoyStreamProvider : IDisposable
    {
        private readonly ManualResetEvent _exitMutex = new ManualResetEvent(false);

        private readonly string _rawDumpFile;
        private TcpClient _tcpClient;
        private Thread _keepaliveThread;

        public PipBoyStreamProvider(string rawDumpFile = null)
        {
            _rawDumpFile = rawDumpFile;
        }

        public Stream Connect(string host, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(host, port);
            Stream stream = _tcpClient.GetStream();
            if (_rawDumpFile != null)
            {
                stream = new CopyInputStream(stream, new FileStream(_rawDumpFile, FileMode.Create), true);
            }
            return ProcessStream(stream, true);
        }

        public Stream ReadFile(string inputFile)
        {
            var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            return ProcessStream(stream, false);
        }

        private Stream ProcessStream(Stream stream, bool keepAlive)
        {
            // read header
            var reader = new BinaryReader(stream);
            var headerMetaPacket = reader.ReadBytes(5);
            var headerPacket = reader.ReadBytes(headerMetaPacket[0]); // ~35 bytes

            if (keepAlive)
            {
                _keepaliveThread = new Thread(SendThread);
                _keepaliveThread.IsBackground = true;
                _keepaliveThread.Start(stream);
            }

            return stream;
        }

        private void SendThread(object streamObj)
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
                catch (Exception)
                {
                    return;
                }
                if (_exitMutex.WaitOne(1000))
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            if (_keepaliveThread != null)
            {
                _exitMutex.Set();
                _keepaliveThread.Join();
                _keepaliveThread = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }
        }
    }
}
