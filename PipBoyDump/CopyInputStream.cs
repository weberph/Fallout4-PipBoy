using System;
using System.IO;

namespace PipBoyDump
{
    public class CopyInputStream : Stream
    {
        private readonly Stream _source;
        private readonly Stream _destination;
        private readonly bool _ownDestination;

        public CopyInputStream(Stream source, Stream destination, bool ownDestination)
        {
            _source = source;
            _destination = destination;
            _ownDestination = ownDestination;
        }

        public override void Flush()
        {
            _source.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            _destination.Write(buffer, offset, read);
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _source.Write(buffer, offset, count);
        }

        public override void Close()
        {
            if (_ownDestination)
            {
                _destination.Close();
            }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (_ownDestination)
            {
                _destination.Dispose();
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => _source.CanRead;
        public override bool CanSeek => _source.CanSeek;
        public override bool CanWrite => _source.CanSeek;
        public override long Length => _source.Length;

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}