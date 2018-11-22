using HackLinks_Server.Util;
using System.IO;

namespace HackLinks_Server.Computers.Filesystems.Temp
{
    internal class TempInode : Inode
    {
        private MemoryStream stream = new MemoryStream();

        public override int Length { get => (int) stream.Length; set => stream.SetLength(value); }

        public override uint Checksum {
            get {
                byte[] bytes = new byte[Length];
                Read(bytes, 0, 0, bytes.Length);
                return HashUtil.CalcMurmur(bytes);
            }
        }

        public TempInode(Filesystem fileSystem, ulong id, int mode) : base(fileSystem, id, mode)
        {

        }

        public override int Read(byte[] buffer, int offset, int position, int count)
        {
            stream.Position = position;
            return stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] inputBuffer, int offset, int position, int count)
        {
            stream.Position = position;
            stream.Write(inputBuffer, offset, count);
        }
    }
}