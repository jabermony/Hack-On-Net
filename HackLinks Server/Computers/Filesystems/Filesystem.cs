using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems
{
    /// <summary>
    /// Represents a filesystem for a computer.
    /// </summary>
    public abstract class Filesystem
    {
        /// <summary>
        /// A unique identifier for this Filesystem within its host.
        /// </summary>
        public abstract ulong ID { get; protected set; }
        public abstract int ComputerID { get; protected set; }

        /// <summary>
        /// Get the root directory
        /// </summary>
        /// <returns>A integer id pointing to the root directory of the filesystem</returns>
        public abstract ulong GetRootId();

        /// <summary>
        /// Get the <see cref="FileHandle"/> at the given path.
        /// </summary>
        /// <param name="path">The absolute path to the file</param>
        /// <returns>The <see cref="FileHandle"/> or null if no such file exists</returns>
        public abstract FileHandle GetFileHandle(string path);

        /// <summary>
        /// Get the group for the file refered to by the given handle.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract Group GetGroup(FileHandle fileHandle);

        /// <summary>
        /// Set the group for the file refered to by the given handle.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="value"></param>
        public abstract void SetGroup(FileHandle fileHandle, Group value);

        /// <summary>
        /// Get the <see cref="FileHandle"/> at the given path, relative to the given file.
        /// </summary>
        /// <param name="path">The absolute path to the file</param>
        /// <returns>The <see cref="FileHandle"/> or null if no such file exists</returns>
        public abstract FileHandle GetFileHandleAt(string path, FileHandle currentDirectory);

        /// <summary>
        ///  Get a integer permission value for the given <see cref="FileHandle"/>.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract int GetPermissions(FileHandle fileHandle);

        /// <summary>
        ///  Set the integer permission value for the given <see cref="FileHandle"/>.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract void SetFilePermissions(FileHandle fileHandle, int newValue);

        /// <summary>
        /// Get the content of the given file as a stream of bytes.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract Stream GetFileContent(FileHandle fileHandle);

        /// <summary>
        /// Get the checksum of the given file's content.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract uint GetFileChecksum(FileHandle fileHandle);

        /// <summary>
        /// Get the OwnerId for the given <see cref="FileHandle"/>.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract int GetOwnerId(FileHandle fileHandle);

        /// <summary>
        /// Set the OwnerId for the given <see cref="FileHandle"/>.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public abstract void SetOwnerId(FileHandle fileHandle, int newId);

        /// <summary>
        /// Get the type for the given fileHandle.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns>The File type.</returns>
        public abstract FileType GetFileType(FileHandle fileHandle);

        /// <summary>
        /// Unlink the given fileHandle from it's parent file, parent is determined by the file handle's path.
        /// </summary>
        /// <param name="fileHandle"></param>
        public abstract void UnlinkFile(FileHandle fileHandle);

        /// <summary>
        /// Create the given file within the given directory
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <param name="permissions"></param>
        /// <param name="ownerId"></param>
        /// <param name="group"></param>
        /// <param name="type"></param>
        public abstract FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group, FileType type);
    }
}
