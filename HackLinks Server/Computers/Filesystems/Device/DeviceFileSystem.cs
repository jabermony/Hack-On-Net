using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Util;

namespace HackLinks_Server.Computers.Filesystems.Device
{
    class DeviceFileSystem : ManagedFileSystem<DeviceInode>
    {
        public DeviceFileSystem(int computerId, ulong id) : base(computerId, id)
        {

        }

        public override void UnlinkFile(FileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        public override FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group, FileType type)
        {
            throw new NotImplementedException();
        }

        public override FileHandle LinkFile(FileHandle directory, string name, ulong filesystemId, ulong inodeID)
        {
            throw new NotImplementedException();
        }
    }
}
