using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HackLinksCommon.NetUtil;

namespace HackLinks_Server.Computers.Processes
{
   public class ProcessSession
    {
        private Process attachedProcess;
        public int ID { get; private set; }

        public GameClient Owner { get; }
        private Node node;

        public ProcessSession(Node node, GameClient owner, Process process)
        {
            this.node = node;
            this.Owner = owner;
            this.attachedProcess = process;
            this.ID = process.ProcessId;
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
            node.DisposeProcessSession(this);
        }

        internal void WriteOutput(Process process, string input)
        {
            if(process.ProcessId == attachedProcess.ProcessId)
            {
                Owner.Send(PacketType.MESSG, input);
            }
        }
    }
}
