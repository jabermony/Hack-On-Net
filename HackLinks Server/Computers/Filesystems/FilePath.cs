using System.Collections.Generic;

namespace HackLinks_Server.Computers.Filesystems
{
    public class FilePath
    {
        private List<string> path;
        private string name;

        public List<string> Path => new List<string>(path);
        public string PathStr => string.Join("/", Path);

        public FilePath(List<string> path)
        {
            this.path = new List<string>(path);
        }

        public FilePath(List<string> path, string name)
        {
            this.path = new List<string>(path)
            {
                name
            };
        }
    }
}