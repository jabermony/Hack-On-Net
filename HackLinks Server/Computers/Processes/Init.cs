using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    class Init : Process
    {
        public Init(int pid, Printer printer, Node computer, Credentials credentials) : base(pid, printer, computer, credentials)
        {
        }

        public override void Run(string command)
        {
            base.Run(command);
            var daemonsFolder = Kernel.GetFile(this, "/daemons");
            if (daemonsFolder == null)
                return;
            var autorunFile = daemonsFolder.GetFile("autorun");
            if (autorunFile == null)
                return;
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
