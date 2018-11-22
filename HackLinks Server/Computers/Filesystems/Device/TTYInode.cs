using HackLinks_Server.Computers.Filesystems.Temp;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HackLinksCommon.NetUtil;

namespace HackLinks_Server.Computers.Filesystems.Device
{
    public class TTYInode : Inode
    {
        public override uint Checksum => 0;

        public override int Length { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private GameClient client;

        public TTYInode(Filesystem fileSystem, ulong id, int mode, GameClient client) : base(fileSystem, id, mode)
        {
            this.client = client;
        }

        public override void Write(byte[] buffer, int offset, int position, int count)
        {
            client.Send(PacketType.MESSG, Encoding.UTF8.GetString(buffer));
        }

        public override int Read(byte[] buffer, int offset, int position, int count)
        {
            return 0;
        }
    }
}
