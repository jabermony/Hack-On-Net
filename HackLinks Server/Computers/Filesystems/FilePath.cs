using System.Collections.Generic;

namespace HackLinks_Server.Computers.Filesystems
{
    public class FilePath
    {
        private List<FileHandle> path;
        public List<FileHandle> Path => new List<FileHandle>(path);

        public FileHandle Parent => path[path.Count > 1 ? path.Count - 2 : 0];

        public FilePath(List<FileHandle> path, FileHandle fileHandle)
        {
            // We create a new instance here to avoid leaking our filehandle early.
            // Otherwise In multithreaded environments it would be possible for an acesssor of the parameter path to touch file handle before it's finished contstruction.
            this.path = new List<FileHandle>(path)
            {
                fileHandle
            };
        }
    }
}