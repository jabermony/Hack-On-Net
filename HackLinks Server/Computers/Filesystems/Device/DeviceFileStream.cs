using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems.Device
{
    class DeviceFileStream : Stream
    {
        private Reader read;
        private Writer write;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 10;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public DeviceFileStream(Reader read, Writer write)
        {
            this.read = read;
            this.write = write;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            write(buffer, offset, count);
        }

        public delegate int Reader(byte[] buffer, int offset, int count);
        public delegate void Writer(byte[] buffer, int offset, int count);
    }
}
