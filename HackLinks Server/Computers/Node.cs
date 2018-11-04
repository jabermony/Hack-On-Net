﻿using HackLinks_Server.Computers.Filesystems;
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

namespace HackLinks_Server.Computers
{
    public class Node
    {
        public static string SERVER_CONFIG_PATH = "/cfg/server.cfg";

        public int id;
        public string ip;

        public int ownerId;

        public IDictionary<ulong,Filesystem> Filesystems { get; } = new Dictionary<ulong,Filesystem>();

        public List<Session> sessions = new List<Session>();
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

        public void Init()
        {
            initProcess = new Init(1, this, new Credentials(0, Group.ROOT));
            RegisterProcess(initProcess);
            initProcess.Run("");
        }

        public Session GetSession(int processId)
        {
            do
            {
                foreach (Session session in sessions)
                {
                    if(session.HasProcessId(processId))
                    {
                        return session;
                    }
                }
                processId = parents.ContainsKey(processId) ? parents[processId] : 0;
            } while (processId != 0);


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
            parents.Add(child, process);
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
                    freedPIDs.Push(processId);
                    // We give all the children away to init process if our process has any
                    if (children.ContainsKey(processId))
                    {
                        foreach (int child in children[processId])
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
