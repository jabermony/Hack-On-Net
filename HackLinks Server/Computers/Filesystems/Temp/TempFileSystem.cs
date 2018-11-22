using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Util;

namespace HackLinks_Server.Computers.Filesystems.Temp
{
    class TempFileSystem : ManagedFileSystem<TempInode>
    {
        public TempFileSystem(int computerId, ulong id) : base(computerId, id)
        {
        }
         
        public override void UnlinkFile(FileHandle parent, FileHandle fileHandle)
        {
            if(parent == null)
            {
                Logger.Error($"Failed to unlink {fileHandle.FilePath.PathStr}");
                return;
            }
            List<FileUtil.DirRecord> directoryRecords = FileUtil.GetDirectoryList(this, parent);

            // we 'cache' these here so we don't have to do the marginally more expensive property get each iteration of the loop.
            string filename = fileHandle.Name;
            ulong inode = fileHandle.Inode;

            // Iterate backwards so that we don't ruin our count on removals
            for (int i = directoryRecords.Count - 1; i >= 0; i--)
            {
                FileUtil.DirRecord rec = directoryRecords[i];
                if (rec.inode.Equals(fileHandle.Inode) && rec.name.Equals(filename))
                {
                    directoryRecords.Remove(rec);
                }
            }
            byte[] bytes = FileUtil.FromDirRecords(directoryRecords.ToArray());
            using (Stream stream = new FilesystemStream(this, parent))
            {
                stream.SetLength(0);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public override FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group, FileType type)
        {
            int mode = ((int)type << 9) | (int)permissions;
            Inode inode = CreateFile(mode);
            inode.OwnerId = ownerId;
            inode.Group = group;

            LinkFile(directory, name, ID, inode.ID);

            return new FileHandle(ID, inode.ID, directory.FilePath.Path, name);
        }

        public override FileHandle LinkFile(FileHandle directory, string name, ulong filesystemId, ulong inodeID)
        {
            List<FileUtil.DirRecord> directoryRecords = FileUtil.GetDirectoryList(this, directory);

            foreach (FileUtil.DirRecord rec in directoryRecords)
            {
                if (rec.name.Equals(name))
                {
                    // file exists return null
                    return null;
                }
            }

            directoryRecords.Add(new FileUtil.DirRecord(filesystemId, inodeID, name));

            byte[] bytes = FileUtil.FromDirRecords(directoryRecords.ToArray());
            GetInode(directory).Write(bytes, 0, 0, bytes.Length);

            return new FileHandle(filesystemId, inodeID, directory.FilePath.Path, name);
        }
    }
}
