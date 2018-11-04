using HackLinks_Server.Util;
using System.IO;

namespace HackLinks_Server.Computers.Filesystems.Temp
{
    internal class TempInode : Inode
    {
        private MemoryStream stream = new MemoryStream();
        public override Stream ContentStream => new TempFileStream(stream);

        public override uint Checksum => HashUtil.CalcMurmur(ContentStream);

        public TempInode(Filesystem fileSystem, ulong id, int mode) : base(fileSystem, id, mode)
        {

        }
    }
}