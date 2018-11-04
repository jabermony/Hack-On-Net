using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems.Temp
{
    public class TempFileStream : Stream
    {
        private MemoryStream datastream;
        private bool isDisposed = false;

        public TempFileStream(MemoryStream datastream)
        {
            this.datastream = datastream;
        }

        public override bool CanRead => datastream.CanRead;

        public override bool CanSeek => datastream.CanSeek;

        public override bool CanWrite => datastream.CanWrite;

        public override long Length => datastream.Length;

        private long position = 0;
        public override long Position
        {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            CheckDisposed();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            datastream.Position = Position;
            int read = datastream.Read(buffer, offset, count);
            Position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            long newpos = offset;

            switch (origin)
            {
                case SeekOrigin.Current:
                    newpos += Position;
                    break;
                case SeekOrigin.End:
                    newpos += datastream.Length;
                    break;
            }

            if(newpos < 0)
            {
                throw new IOException("An attempt was made to move the position before the beginning of the stream");
            }

            return position = newpos;
        }

        public override void SetLength(long value)
        {
            CheckDisposed();
            datastream.SetLength(value);
        }

        private void CheckDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access a closed file.");
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            datastream.Position = Position;
            datastream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            isDisposed = true;
            base.Dispose(disposing);
        }
    }
}
