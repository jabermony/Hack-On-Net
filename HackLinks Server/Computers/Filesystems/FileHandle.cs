using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems
{
    public class FileHandle
    {
        /// <summary>
        /// The ID of the <see cref="FilesystemId"/> this file belongs to
        /// </summary>
        public ulong FilesystemId { get; }

        /// <summary>
        /// The inode number for this file, it should usually be unique per <see cref="FilesystemId"/>
        /// </summary>
        public ulong Inode { get; }

        /// <summary>
        /// The absolute path used to open this file.
        /// </summary>
        public FilePath FilePath { get; }

        /// <summary>
        /// The filename. Strictly the string following the last non-trailing forward slash in Path.
        /// </summary>
        public string Name { get; }

        public FileHandle(Filesystem filesystem, ulong inode, List<FileHandle> path, string name)
        {
            FilesystemId = filesystem.ID;
            Inode = inode;
            Name = name;
            FilePath = new FilePath(path, this);
        }
    }
}
