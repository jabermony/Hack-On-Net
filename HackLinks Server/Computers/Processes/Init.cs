using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Files;
using HackLinks_Server.Util;

namespace HackLinks_Server.Computers.Processes
{
    class Init : Process
    {
        public Init(int pid, Node computer, Credentials credentials) : base(pid, computer, credentials)
        {
        }

        public override void Run(string command)
        {
            Filesystem.Error error = Filesystem.Error.None;
            base.Run(command);
            var rootFile = Kernel.Open(this, "/", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                // Error setting up die
                Signal(ProcessSignal.SIGKILL);
                Logger.Error("Error setting up Init, Root unavailable due to : {0}", error);
                return;
            }

            var daemonsFolder = Kernel.Open(this, "/daemons", FileDescriptor.Flags.Read | FileDescriptor.Flags.Create_Open, Filesystems.Permission.O_All | Filesystems.Permission.G_All | Filesystems.Permission.U_Read, ref error);
            if (error != Filesystem.Error.None)
            {
                // Error setting up die
                Signal(ProcessSignal.SIGKILL);
                Logger.Error("Error setting up Init, /daemons unavailable due to : {0}", error);
                return;
            }

            FileDescriptor autorunFile = Kernel.Open(this, "/daemons/autorun", FileDescriptor.Flags.Read | FileDescriptor.Flags.Create_Open, Filesystems.Permission.O_All | Filesystems.Permission.G_All | Filesystems.Permission.U_Read, ref error);
            if (error != Filesystem.Error.None)
            {
                // Error setting up die
                Signal(ProcessSignal.SIGKILL);
                Logger.Error("Error setting up Init, /daemons/autorun unavailable due to : {0}", error);
                return;
            }

            string content = Kernel.GetContent(autorunFile, ref error);

            if (content == null || error != Filesystem.Error.None)
                return;

            foreach (string line in content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                FileDescriptor daemonFd = Kernel.OpenAt(daemonsFolder, line, FileDescriptor.Flags.None, Permission.None, ref error);
                File daemonFile = new File(daemonFd, Kernel);
                if (daemonFile == null || error != Filesystem.Error.None)
                    continue;
                if (!daemonFile.HasExecutePermission(Credentials))
                    continue;
                //TODO user credentials from autorun file
                Kernel.StartProcess(this, daemonFile);
            }
        }
    }
}
