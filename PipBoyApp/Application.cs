using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PipBoy;
using PipBoyApp.Debugging;

namespace PipBoyApp
{
    public class Startup
    {
        [STAThread]
        public static void Main()
        {
            var app = new PipBoyApplication();
            app.Run(new MainWindow());
        }
    }

    public class PipBoyApplication : Application
    {
        private readonly ManualResetEvent _startProcessing = new ManualResetEvent(false);
        private readonly Semaphore _exitMutex = new Semaphore(0, int.MaxValue);
        private readonly Thread _ioThread;
        private Thread _keepaliveThread;
        private MainWindow _window;

        public PipBoyApplication()
        {
            _ioThread = new Thread(IoThread);
            _ioThread.Start();
        }

        public new int Run(Window window)
        {
            _window = (MainWindow)window;
            _startProcessing.Set();
            return base.Run(window);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _exitMutex.Release(int.MaxValue);
            _ioThread.Join();
            _keepaliveThread?.Join();
        }

        private void IoThread()
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
        }

        private void ReadStream(Stream stream, bool keepAlive)
        {
            // read header
            var reader = new BinaryReader(stream);
            var headerMetaPacket = reader.ReadBytes(5);
            var headerPacket = reader.ReadBytes(headerMetaPacket[0]); // ~35 bytes

            if (keepAlive)
            {
                _keepaliveThread = new Thread(KeepaliveThread);
                _keepaliveThread.Start(stream);
            }

            var gameStateReader = new GameStateReader(stream);

            _startProcessing.WaitOne();
            while (!_exitMutex.WaitOne(0) && gameStateReader.NextState())
            {
                _window.OnGameStateChanged(gameStateReader.GameState);
            }

            _window.OnFinished();
        }

        private void KeepaliveThread(object streamObj)
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
                if (_exitMutex.WaitOne(1000))
                {
                    return;
                }
            }
        }
    }
}
