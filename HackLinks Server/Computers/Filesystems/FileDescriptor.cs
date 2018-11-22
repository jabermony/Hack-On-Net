using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Filesystems
{
    public class FileDescriptor
    {
        /// <summary>
        /// The ID of the <see cref="FilesystemId"/> this file belongs to
        /// </summary>
        public ulong FilesystemId { get; }

        /// <summary>
        /// The ID for this FileDescriptor, it should usually be unique per <see cref="Node"/>
        /// </summary>
        public ulong ID { get; }

        /// <summary>
        /// The absolute path used to open this file.
        /// </summary>
        public FilePath FilePath { get; }

        /// <summary>
        /// The filename. Strictly the string following the last non-trailing forward slash in Path.
        /// </summary>
        public string Name { get; }

        public FileDescriptor(ulong FilesystemId, ulong id, List<string> path)
        {
            this.FilesystemId = FilesystemId;
            ID = id;
            Name = PathUtil.Basename(path);
            FilePath = new FilePath(path);
        }

        [Flags]
        public enum Flags
        {
            None        = 0b00000000,
            Read        = 0b00000001,
            Write       = 0b00000010,
            Read_Write  = 0b00000011,
            Create_Open = 0b00000100,
        }
    }
}
