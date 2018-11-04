using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems.Disk
{
    class DBFileStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => Server.Instance.DatabaseLink.GetFileLength(computerId, filesystemId, inode);

        private long position = 0;

        private ulong filesystemId;
        private ulong inode;
        private int computerId;

        public DBFileStream(FileHandle handle)
        {
            this.filesystemId = handle.FilesystemId;
            this.inode = handle.Inode;
        }

        public DBFileStream(int computerId, ulong filesystemId, ulong inode)
        {
            this.filesystemId = filesystemId;
            this.inode = inode;
            this.computerId = computerId;
        }

        public override long Position {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        // TODO buffering might improve speed
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // We have to cast to an int here because the underlying DB call returns a long but streams require an int.
            // We avoid issues with truncation as we can't actually read more than an int worth of a data anyway as our count is an int.
            int numOfBytes = (int)Server.Instance.DatabaseLink.ReadFile(computerId, filesystemId, inode, buffer, offset, position, count);
            position += numOfBytes;
            return numOfBytes;
        }

        // TODO seek past end to expand underlying blob. Maybe call SetLength if we exceed length ?
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newpos = position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newpos = offset;
                    break;
                case SeekOrigin.Current:
                    newpos = Position + offset;
                    break;
                case SeekOrigin.End:
                    newpos = Length + offset;
                    break;
            }

            if (newpos < 0)
            {
                throw new IOException("An attempt was made to move the position before the beginning of the stream");
            }

            position = newpos;

            return position;
        }

        public override void SetLength(long newLength)
        {
            Server.Instance.DatabaseLink.SetFileLength(computerId, filesystemId, inode, newLength);
            if(position >= newLength)
            {
                position = newLength - 1;
            }
        }

        public override void Write(byte[] inputBuffer, int offset, int count)
        {
            byte[] writeBuffer = new byte[inputBuffer.Length - offset];
            Array.Copy(inputBuffer, offset, writeBuffer, 0, writeBuffer.Length);

            Server.Instance.DatabaseLink.WriteFile(computerId, filesystemId, inode, writeBuffer, position);
            position += count;
        }
    }
}
