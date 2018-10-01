using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
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
    }
}
