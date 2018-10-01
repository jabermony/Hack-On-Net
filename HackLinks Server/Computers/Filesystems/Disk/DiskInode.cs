using HackLinks_Server.Computers.Filesystems.Disk;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems
{
    public class DiskInode : Inode
    {
        public string Content { get; set; }
        //TODO maybe cache this? if it's worth it.
        public override Stream ContentStream => new DBFileStream(Filesystem.ComputerID, Filesystem.ID, ID);

        public override uint Checksum => HashUtil.CalcMurmur(ContentStream);

        public DiskInode(Filesystem fileSystem, ulong id, int mode) : base(fileSystem, id, mode)
        {

        }

    }
}
