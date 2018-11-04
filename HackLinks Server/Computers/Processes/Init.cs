using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    class Init : Process
    {
        public Init(int pid, Node computer, Credentials credentials) : base(pid, computer, credentials)
        {
        }

        public override void Run(string command)
        {
            base.Run(command);
            var rootFile = Kernel.GetFile(this, "/");
            rootFile.GetFile("daemons");
            var daemonsFolder = Kernel.GetFile(this, "/daemons");
            if (daemonsFolder == null)
            {
                Kernel.CreateFile(Kernel.GetFile(this, "/").FileHandle, "daemons", Filesystems.Permission.O_All | Filesystems.Permission.G_All | Filesystems.Permission.U_Read, Credentials.UserId, Credentials.Group, Filesystems.FileType.Directory);
                daemonsFolder = Kernel.GetFile(this, "/daemons");
            }

            var autorunFile = daemonsFolder.GetFile("autorun");
            if (autorunFile == null)
            {
                Kernel.CreateFile(daemonsFolder.FileHandle, "autorun", Filesystems.Permission.O_All | Filesystems.Permission.G_All | Filesystems.Permission.U_Read, Credentials.UserId, Credentials.Group);
                autorunFile = daemonsFolder.GetFile("autorun");
            }
                
            string content = autorunFile.GetContent();
            if (content == null)
                return;
            foreach (string line in content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var daemonFile = daemonsFolder.GetFile(line);
                if (daemonFile == null)
                    continue;
                if (!daemonFile.HasExecutePermission(Credentials))
                    continue;
                //TODO user credentials from autorun file
                Kernel.StartProcess(this, daemonFile);
            }
        }
    }
}
