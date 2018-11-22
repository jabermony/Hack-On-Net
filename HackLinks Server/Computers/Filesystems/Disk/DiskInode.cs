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
        public override uint Checksum
        {
            get
            {
                byte[] bytes = new byte[Length];
                Read(bytes, 0, 0, bytes.Length);
                return HashUtil.CalcMurmur(bytes);
            }
        }

        public override int Length { get => Server.Instance.DatabaseLink.GetFileLength(ComputerID, FileSystemId, ID); set => Server.Instance.DatabaseLink.SetFileLength(ComputerID, FileSystemId, ID, value); }

        public override int Read(byte[] buffer, int offset, int position, int count)
        {
            // We have to cast to an int here because the underlying DB call returns a long but streams require an int.
            // We avoid issues with truncation as we can't actually read more than an int worth of a data anyway as our count is an int.
            int numOfBytes = (int)Server.Instance.DatabaseLink.ReadFile(ComputerID, FileSystemId, ID, buffer, offset, position, count);
            return numOfBytes;
        }

        public override void Write(byte[] inputBuffer, int offset, int position, int count)
        {
            byte[] writeBuffer = new byte[inputBuffer.Length - offset];
            Array.Copy(inputBuffer, offset, writeBuffer, 0, writeBuffer.Length);

            Server.Instance.DatabaseLink.WriteFile(ComputerID, FileSystemId, ID, writeBuffer, position);
        }

        public DiskInode(Filesystem fileSystem, ulong id, int mode) : base(fileSystem, id, mode)
        {

        }

    }
}
