using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Daemons;
using HackLinks_Server.Computers.Processes.Daemons;
using HackLinks_Server.Files;
using HackLinks_Server.Util;
using HackLinksCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Filesystems.Device;
using HackLinks_Server.Computers.Filesystems.Temp;

namespace HackLinks_Server.Computers
{
    public class Node
    {
        public static string SERVER_CONFIG_PATH = "/cfg/server.cfg";

        public int id;
        public string ip;

        public int ownerId;

        public IDictionary<ulong,Filesystem> Filesystems { get; } = new Dictionary<ulong,Filesystem>();

        public IDictionary<ulong, Filesystem> Filesystems { get; } = new Dictionary<ulong, Filesystem>();

        public IDictionary<int, int> SessionIds { get; } = new Dictionary<int, int>();
        public List<Session> sessions = new List<Session>();


        internal void AttachSession(Session session)
        {
            TTYInode inode = new TTYInode(Filesystems[1], 1, DBUtil.GenerateMode(FileType.Regular, Permission.A_All), session.owner);
            deviceSystem.RegisterNewFile(inode);
            terminals.Add(inode);
            FileHandle devfh = Filesystems[0].GetFileHandle("/dev");
            Filesystems[devfh.FilesystemId].LinkFile(devfh, "tty"+ terminals.IndexOf(inode), Filesystems[1].ID, inode.ID);
            sessions.Add(session);
        }

        internal void DetachSession(Session session)
        {
            sessions.Remove(session);
        }

        private Dictionary<int, ProcessSession> processSessions = new Dictionary<int, ProcessSession>();

        internal void DisposeProcessSession(ProcessSession processSession)
        {
            int id = processSession.ID;
            processSessions.Remove(id);
            // We do this here as as Session IDs are alocated as the PID of the first process in the session but persist after the process dies.
            // PIDs re-enter the pool after their session dies.
            freedPIDs.Push(id);
        }

        public List<Daemon> daemons = new List<Daemon>();
        public List<Log> logs = new List<Log>();

        public Kernel Kernel { get; private set; }

        private Dictionary<int, Process> processes = new Dictionary<int, Process>();

        public Stack<int> freedPIDs = new Stack<int>();
        private int nextPID = 2;
        public int NextPID => freedPIDs.Count > 0 ? freedPIDs.Pop() : nextPID++;

        private Dictionary<int, int> parents = new Dictionary<int, int>();
        private Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();
        private Process initProcess;

        public Node()
        {
            Kernel = new Kernel(this);
        }

        // TODO allow existing process to branch off into a new session
        internal ProcessSession CreateProcessSession(string type, Credentials credentials, GameClient gameClient)
        {
            Process process = CreateProcess(type, credentials);
            ProcessSession session = new ProcessSession(this, gameClient, process);
            processSessions.Add(process.ProcessId, session);
            return session;
        }

        public void Init()
        {
            TempFileSystem tempFileSystem = new TempFileSystem(id, 1);
            Filesystems[1] = tempFileSystem;
            DeviceFileSystem deviceSystem = new DeviceFileSystem(id, 2);
            Filesystems[2] = deviceSystem;

            tempFileSystem.RegisterNewFile(new TempInode(tempFileSystem, 1, DBUtil.GenerateMode(FileType.Directory, Permission.A_All)));
            Filesystems[0].LinkFile(Filesystems[0].GetFileHandle("/"), "tmp", 1, 1);
            TempInode dev = new TempInode(tempFileSystem, 2, DBUtil.GenerateMode(FileType.Directory, Permission.O_All | Permission.A_Execute | Permission.A_Read))
            {
                OwnerId = 0,
                Group = Group.ROOT
            };
            tempFileSystem.RegisterNewFile(dev);
            Filesystems[0].LinkFile(Filesystems[0].GetFileHandle("/"), "dev", tempFileSystem.ID, 2);

            initProcess = new Init(1, this, new Credentials(0, Group.ROOT));
            RegisterProcess(initProcess);

            initProcess.Run("");
        }

        internal int GetSessionId(int processId)
        {
            return SessionIds[processId];
        }

        public Process CreateProcess(string type, Process parent)
        {
            Process child = CreateProcess(type, parent.Credentials);
            // Set child up in the parent's session
            SessionIds[child.ProcessId] = parent.SessionId;
            SetupChildProcess(parent, child);
            return child;
        }

        public Process SetupChildProcess(Process process, Process child)
        {
            SetChildProcess(process, child);
            child.ActiveDirectory = process.ActiveDirectory;
            return child;
        }

        private Process CreateProcess(string type, Credentials credentials)
        {
            return CreateProcess(Type.GetType($"HackLinks_Server.Computers.Processes.{type}"), credentials);
        }

        private Process CreateProcess(Type type, Credentials credentials)
        {
            Logger.Debug(type);
            object[] args;
            if (type == typeof(ServerAdmin) || type.IsInstanceOfType(typeof(Daemon)))
            {
                args = new object[] { NextPID, this, credentials, this };
            }
            else
            {
                args = new object[] { NextPID, this, credentials };
            }
            Process process = (Process)Activator.CreateInstance(type, args);

            if (type.IsInstanceOfType(typeof(Daemon)))
            {
                daemons.Add((Daemon)process);
            }
            // Assign SessionId
            SessionIds[process.ProcessId] = process.ProcessId;
            return process;
        }

        public ProcessSession GetProcessSession(int processId)
        {
            if (SessionIds.ContainsKey(processId))
            {
                return processSessions[SessionIds[processId]];
            }

            return null;
        }

        public string GetDisplayName()
        {
           var cfgFile = Kernel.GetFile(initProcess, SERVER_CONFIG_PATH);
            if (cfgFile == null)
                return ip;
            var lines = cfgFile.GetContent().Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None); ;
            foreach (var line in lines)
            {
                if (line.StartsWith("name="))
                    return line.Substring(5);
            }
            return ip;
        }

        internal ulong GetRootFilesystemId()
        {
            return 0;
        }

        public Daemon GetDaemon(string type)
        {
            foreach(Daemon daemon in daemons)
                if (daemon.IsOfType(type))
                    return daemon;
            return null;
        }

        public void SetChildProcess(Process process, Process child)
        {
            SetChildProcess(process.ProcessId, child.ProcessId);
        }

        protected void SetChildProcess(int process, int child)
        {
            if (parents.ContainsKey(child))
            {
                children[parents[child]].Remove(child);
            }
            if (!children.ContainsKey(process))
            {
                children.Add(process, new List<int>());
            }
            parents[child] = process;
            children[process].Add(child);
        }

        internal void RegisterProcess(Process process)
        {
            processes[process.ProcessId] = process;
        }

        internal void NotifyProcessStateChange(int processId, Process.State newState)
        {
            switch (newState)
            {
                case Process.State.Dead:
                    int parentId = GetParentId(processId);
                    if (parentId > 1)
                    {
                        processes[parentId].NotifyDeadChild(processes[processId]);
                        children[parentId].Remove(processId);
                        parents.Remove(processId);
                    }
                    processes.Remove(processId);
                    // We give all the children away to init process if our process has any
                    if (children.ContainsKey(processId))
                    {
                        List<int> oldChildren = new List<int>(children[processId]);
                        foreach (int child in oldChildren)
                        {
                            SetChildProcess(1, child);
                        }
                    }
                    break;
            }
        }

        public int GetParentId(int pid)
        {
            return parents.ContainsKey(pid) ? parents[pid] : 1;
        }
    }
}
