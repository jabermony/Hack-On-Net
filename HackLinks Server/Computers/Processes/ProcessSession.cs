using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
   public class ProcessSession
    {
        private Process attachedProcess;
        public GameClient Owner { get; }

        public ProcessSession(GameClient owner, Process process)
        {
            this.Owner = owner;
            this.attachedProcess = process;
        }

        public bool HasProcessId(int pid)
        {
            return attachedProcess.ProcessId == pid;
        }

        public void AttachProcess(Process process)
        {
            attachedProcess = process;
        }


        public void WriteInput(string inputData)
        {
            attachedProcess.WriteInput(inputData);
        }

        public void DisconnectSession()
        {
            // Tell the attachedProcess (usually Shell. That we're going away)
            attachedProcess.Signal(Process.ProcessSignal.SIGHUP);
        }
    }
}
