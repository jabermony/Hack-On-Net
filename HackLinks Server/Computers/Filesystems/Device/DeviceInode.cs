using HackLinks_Server.Computers.Filesystems.Device;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems
{
    public class DeviceInode : Inode
    {
        public string Content { get; set; }

        static string test = "1234567890";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(test);

        public override Stream ContentStream => new DeviceFileStream(
            (byte[] buffer, int count, int offset) => { bytes.CopyTo(buffer, 0);  return 0; },
            (byte[] buffer, int count, int offset) => { Console.WriteLine("XXXDEBUG " + System.Text.Encoding.UTF8.GetString(buffer)); });

        public override uint Checksum => HashUtil.CalcMurmur(ContentStream);

        public DeviceInode(Filesystem fileSystem, ulong id, int mode) : base(fileSystem, id, mode)
        {

        }

    }
}
