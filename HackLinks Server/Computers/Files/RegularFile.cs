using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Files;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using MySql.Data.MySqlClient;

namespace HackLinks_Server.Files
{
    public class RegularFile : File
    {
        private int checksum;
        public override int Checksum => checksum;
        private string content = "";
        public override string Content { get => content; set { content = value; Dirty = true; checksum = content.GetHashCode(); } } // TODO make hash function portable/low collision eg. https://softwareengineering.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed

        protected RegularFile(int id, Node computer, File parent, string name)
            : base(id, computer, parent, name)
        {

        }

        /// <summary>
        /// Create a new file and register it a new file id with the given <see cref="FileSystemManager"/>
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="computer"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static File CreateNewFile(FileSystemManager manager, Node computer, File parent, string name)
        {
            File newFile = new RegularFile(manager.GetNewFileId(), computer, parent, name);
            manager.RegisterNewFile(newFile);
            return newFile;
        }

        /// <summary>
        /// Attempt to create a new file with the given id and register it with the given <see cref="FileSystemManager"/>
        /// It's usually better to use <see cref="CreateNewFile(FileSystemManager, Node, File, string)"/> unless you need to explicitly specify the file id.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="computer"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown when the id is already registered</exception>
        public static File CreateNewFile(int id, FileSystemManager manager, Node computer, File parent, string name)
        {
            if (manager.IsIdInUse(id))
            {
                throw new ArgumentException($"File id \"{id}\" is already in use");
            }

            File newFile = new RegularFile(id, computer, parent, name);
            manager.RegisterNewFile(newFile);
            return newFile;
        }

        public static File CreateNewFolder(FileSystemManager manager, Node computer, File parent, string name)
        {
            File newFile = new RegularFile(manager.GetNewFileId(), computer, parent, name);
            newFile.Type = FileType.Directory;
            manager.RegisterNewFile(newFile);
            return newFile;
        }
    }
}
