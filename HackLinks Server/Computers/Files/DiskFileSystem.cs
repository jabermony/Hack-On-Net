using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Files
{
    /// <summary>
    /// Contains Files for a computer
    /// </summary>
    public class DiskFileSystem
    {
        public readonly FileSystemManager fileSystemManager;

        public File rootFile;

        public DiskFileSystem(FileSystemManager fileSystemManager)
        {
            this.fileSystemManager = fileSystemManager;
        }

        public File CreateFile(Node computer, File parent, string fileName)
        {
            return RegularFile.CreateNewFile(fileSystemManager, computer, parent, fileName);
        }

        public File CreateFile(int id, Node computer, File parent, string fileName)
        {
            return RegularFile.CreateNewFile(id, fileSystemManager, computer, parent, fileName);
        }

        public File CreateFolder(Node computer, File parent, string fileName)
        {
            return RegularFile.CreateNewFolder(fileSystemManager, computer, parent, fileName);
        }
    }
}
