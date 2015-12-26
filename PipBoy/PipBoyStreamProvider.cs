using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using PipBoy.Debugging;

namespace PipBoy
{
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
            var packetType = headerMetaPacket[4];
            if (packetType == 0x02)
            {
                // busy
                stream.Close();
                throw new PipBoyBusyException();
            }

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

    public class PipBoyBusyException : Exception
    {
    }
}