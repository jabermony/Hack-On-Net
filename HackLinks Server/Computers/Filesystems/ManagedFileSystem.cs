using System;
using System.Collections.Generic;
using System.IO;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Util;

namespace HackLinks_Server.Computers.Filesystems
{
    /// <summary>
    /// Contains the global list of all files and alocates ids to new files.
    /// </summary>
    public abstract class ManagedFileSystem<T> : Filesystem where T : Inode
    {
        public override ulong ID { get; protected set; }
        public override int ComputerID { get; protected set; }

        private Dictionary<ulong, T> fileMap = new Dictionary<ulong, T>();

        private ulong fileCounter = 1;

        protected ManagedFileSystem(int computerID, ulong id)
        {
            Type type = typeof(T);
            if (type.IsInterface || type.IsAbstract)
                throw new ArgumentException($"Filesystem Inode declated with un-instantiable type: {GetType()}");
            ID = id;
            ComputerID = computerID;
        }

        public override ulong GetRootId()
        {
            return 1;
        }

        /// <summary>
        /// Return a new globally unique file id
        /// </summary>
        /// <returns>globally unique file id</returns>
        private ulong GetNewFileId()
        {
            while(fileMap.ContainsKey(fileCounter))
            {
                fileCounter++;
            }
            return fileCounter;
        }

        /// <summary>
        /// Register the given file. Existing files with the same id will be overriden.
        /// </summary>
        /// <param name="newFile">File to register</param>
        public void RegisterNewFile(T newFile)
        {
            Logger.Info($"{newFile.ID} Registered with id {newFile.ID}");
            fileMap[newFile.ID] = newFile;
        }

        /// <summary>
        /// Register the given files. Existing files with the same id will be overriden.
        /// </summary>
        /// <param name="newFile">File to register</param>
        public void RegisterNewFiles(List<T> newFile)
        {
            foreach(T node in newFile)
            {
                RegisterNewFile(node);
            }
        }

        protected T CreateFile(int mode)
        {
            ulong id = GetNewFileId();
            fileMap.Add(id, (T) Activator.CreateInstance(typeof(T), this, id, mode));
            return fileMap[id];
        }

        /// <summary>
        /// Returns the inode associated with the given <see cref="FileHandle"/> or null if no such Inode exists.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        protected Inode GetInode(FileHandle fileHandle)
        {
            if(!fileMap.ContainsKey(fileHandle.Inode))
            {
                return null;
            }
            return fileMap[fileHandle.Inode];
        }

        private FileHandle GetRootHandle()
        {
            return new FileHandle(ID, 1, new List<FileHandle>(), "/");
        }

        public override FileHandle GetFileHandle(string path)
        {
            return GetFileHandleAt(path, GetRootHandle());
        }

        public override FileHandle GetFileHandleAt(string inPath, FileHandle currentDirectory)
        {
            // A check for absolute paths. If we're absolute with a relative invocation then we'll call out.
            if (inPath.StartsWith("/") && currentDirectory.Name != "/")
            {
                return GetFileHandle(inPath);
            }

            string[] parts = inPath.Split('/');

            for (int pos = 0; pos < parts.Length; pos++)
            {
                string next = parts[pos];

                if (next.Equals(".") || next.Equals(""))
                {
                    continue;
                }

                if (next.Equals(".."))
                {
                    currentDirectory = currentDirectory.FilePath.Parent;
                    continue;
                }

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
                if (nextDirectory == null)
                {
                    return null;
                }
                else
                {
                    currentDirectory = nextDirectory;
                }
            };
            return currentDirectory;
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
            if (inode != null)
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

        public override void LinkFile(FileHandle parent, FileHandle fileHandle, string name)
        {
            LinkFile(parent, name, fileHandle.FilesystemId, fileHandle.Inode);
        }
    }
}
