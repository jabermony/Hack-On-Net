using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems
{
    public class FilesystemStream : Stream
    {
        private bool isDisposed = false;

        public FilesystemStream(Filesystem filesystem, FileHandle handle)
        {
            this.filesystem = filesystem;
            this.handle = handle;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => filesystem.GetFileLength(handle, ref lastError);

        private Filesystem.Error lastError;

        public Filesystem.Error LastError => lastError;

        private long position = 0;
        private Filesystem filesystem;
        private FileHandle handle;

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
            // TODO should position really be cast to an int? probably not...
            int read = filesystem.ReadFile(handle, buffer, offset, (int) Position, count, ref lastError);
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
                    newpos += Length;
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
            filesystem.SetFileLength(handle, (int) value, ref lastError);
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
            filesystem.WriteFile(handle, buffer, offset, (int) Position, count, ref lastError);
        }

        protected override void Dispose(bool disposing)
        {
            isDisposed = true;
            base.Dispose(disposing);
        }
    }
}
