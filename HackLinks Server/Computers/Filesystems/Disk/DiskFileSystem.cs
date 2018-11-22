using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace HackLinks_Server.Computers.Filesystems
{
    /// <summary>
    /// Contains Files for a computer
    /// </summary>
    public class DiskFileSystem : ManagedFileSystem<DiskInode>
    {
        public DiskFileSystem(int computerId, ulong id) : base(computerId, id)
        {
        }

        public override void UnlinkFile(FileHandle parent, FileHandle fileHandle)
        {
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
                stream.Write(bytes, 0, bytes.Length);
            }
            Server.Instance.DatabaseLink.UnlinkFile(ComputerID, parent, fileHandle);
        }

        public override FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group, FileType type)
        {
            int mode = ((int)type << 9) | (int)permissions;
            DiskInode inode = CreateFile(mode);
            inode.OwnerId = ownerId;
            inode.Group = group;

            Server.Instance.DatabaseLink.CreateFile(ComputerID, ID, inode.ID, 0, mode, (int)group, ownerId, null);

            LinkFile(directory, name, ID, inode.ID);

            return new FileHandle(ID, inode.ID, directory.FilePath.Path, name);
        }

        public override FileHandle LinkFile(FileHandle directory, string name,ulong filesystemId, ulong inodeID)
        {
            List<FileUtil.DirRecord> directoryRecords = FileUtil.GetDirectoryList(this, directory);
            foreach (FileUtil.DirRecord rec in directoryRecords)
            {
                if (rec.name.Equals(name))
                {
                    return null;
                }
            }
            directoryRecords.Add(new FileUtil.DirRecord(filesystemId, inodeID, name));
            byte[] bytes = FileUtil.FromDirRecords(directoryRecords.ToArray());
            using (Stream stream = new FilesystemStream(this, directory))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            // TODO handle links for other FS?
            Server.Instance.DatabaseLink.LinkFile(ComputerID, ID, inodeID);
            return new FileHandle(filesystemId, inodeID, directory.FilePath.Path, name);
        }

    }
}
