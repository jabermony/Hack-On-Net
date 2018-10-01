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

        private FileHandle GetRootHandle()
        {
            return new FileHandle(this, 0, "/");
        }

        public override FileHandle GetFileHandle(string path)
        {
            return GetFileHandleAt(path, new FileHandle(this, 1, "/"));
        }

        public override FileHandle GetFileHandleAt(string inPath, FileHandle currentDirectory)
        {
            // A check for absolute paths. If we're absolute with a relative invocation then we'll call out.
            if(inPath.StartsWith("/") && !currentDirectory.Path.Equals("/"))
            {
                return GetFileHandle(inPath);
            }
                
            string path = PathUtil.Normalize(inPath, currentDirectory.Path);
            string[] parts;
            if (path.StartsWith(currentDirectory.Path) && !currentDirectory.Path.Equals("/"))
            {
                parts = path.Substring(currentDirectory.Path.Length).Split('/');
            } else
            {
                parts = path.Split('/');
            }

            for(int pos = path.StartsWith("/") ? 1 : 0; pos < parts.Length; pos++)
            { 
                if (path.Equals(currentDirectory.Path))
                {
                    return currentDirectory;
                }
                string next = parts[pos];

                List<FileHandle> files = FileUtil.GetDirectoryFileHandles(this, currentDirectory);
                FileHandle nextDirectory = null;
                foreach (FileHandle handle in files)
                {
                    if (handle.Name.Equals(next))
                    {
                        nextDirectory = handle;
                        break;
                    }
                }
                if(nextDirectory == null)
                {
                    return null;
                } else
                {
                    currentDirectory = nextDirectory;
                    if (path.Equals(currentDirectory.Path))
                    {
                        return currentDirectory;
                    }
                }
            };
            return null;
        }

        public override int GetPermissions(FileHandle fileHandle)
        {
            Inode inode = GetInode(fileHandle);
            return inode.PermissionValue;
        }

        public override void SetFilePermissions(FileHandle fileHandle, int value)
        {
            Inode inode = GetInode(fileHandle);
            inode.PermissionValue = value;
        }

        public override Stream GetFileContent(FileHandle fileHandle)
        {
            return GetInode(fileHandle).ContentStream;
        }

        public override int GetOwnerId(FileHandle fileHandle)
        {
            Inode inode = GetInode(fileHandle);
            return inode.OwnerId;
        }

        public override void SetOwnerId(FileHandle fileHandle, int newId)
        {
            Inode inode = GetInode(fileHandle);
            inode.OwnerId = newId;
        }

        public override Group GetGroup(FileHandle fileHandle)
        {
            Inode inode = GetInode(fileHandle);
            return inode.Group;
        }

        public override void SetGroup(FileHandle fileHandle, Group value)
        {
            Inode inode = GetInode(fileHandle);
            inode.Group = value;
        }

        public override FileType GetFileType(FileHandle fileHandle)
        {
            Inode inode = GetInode(fileHandle);
            if(inode != null)
            {
                return inode.Type;
            }
            else
            {
                // Invalid type?
                return FileType.Regular;
            }
        }

        public override uint GetFileChecksum(FileHandle fileHandle)
        {
            return GetInode(fileHandle).Checksum;
        }

        public override void UnlinkFile(FileHandle fileHandle)
        {
            FileHandle parent = GetFileHandle(PathUtil.Dirname(fileHandle.Path));

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
            GetFileContent(parent).Write(bytes, 0, bytes.Length);
            Server.Instance.DatabaseLink.UnlinkFile(ComputerID, parent, fileHandle);
        }

        public override FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group, FileType type)
        {
            int mode = ((int)type << 9) | (int)permissions;
            DiskInode inode = CreateFile(mode);
            inode.OwnerId = ownerId;
            inode.Group = group;

            Server.Instance.DatabaseLink.CreateFile(ComputerID, ID, inode.ID, 0, mode, (int)group, ownerId, null);

            Link(directory, name, ID, inode.ID);

            return new FileHandle(this, inode.ID, $"{directory.Path}/{name}");
        }

        public void Link(FileHandle directory, string name,ulong filesystemId, ulong inodeID)
        {
            List<FileUtil.DirRecord> directoryRecords = FileUtil.GetDirectoryList(this, directory);
            directoryRecords.Add(new FileUtil.DirRecord(filesystemId, inodeID, name));
            byte[] bytes = FileUtil.FromDirRecords(directoryRecords.ToArray());
            GetFileContent(directory).Write(bytes, 0, bytes.Length);
            Server.Instance.DatabaseLink.LinkFile(ComputerID, ID, inodeID);
        }
    }
}
