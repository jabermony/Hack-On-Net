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

        private DeviceFileSystem deviceSystem;
        private TempFileSystem tempFileSystem;
        private TempFileSystem pipeFileSystem;

        public IDictionary<ulong, Filesystem> Filesystems { get; } = new Dictionary<ulong, Filesystem>();

        public IDictionary<int, int> SessionIds { get; } = new Dictionary<int, int>();
        public List<Session> sessions = new List<Session>();

        public List<TTYInode> terminals = new List<TTYInode>();

        internal void AttachSession(Session session)
        {
            TTYInode inode = new TTYInode(Filesystems[1], 1, DBUtil.GenerateMode(FileType.Regular, Permission.A_All), session.owner);
            deviceSystem.RegisterNewFile(inode);
            terminals.Add(inode);
            FileHandle devfh = Filesystems[0].GetFileHandle("/dev");
            Filesystems[devfh.FilesystemId].LinkFile(devfh, "tty"+ terminals.IndexOf(inode), Filesystems[1].ID, inode.ID);
            sessions.Add(session);
        }

        internal GameClient GetClient(Process process)
        {
            ProcessSession session = GetProcessSession(process.SessionId) ?? null;
            return session?.Owner;
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

        internal List<FileUtil.DirRecord> getDirectoryList(FileDescriptor fileDescriptor)
        {
            if(OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                return FileUtil.GetDirectoryList(Filesystems[handle.FilesystemId], handle);
            }
            return null;
        }

        public List<Daemon> daemons = new List<Daemon>();
        public List<Log> logs = new List<Log>();

        public Kernel Kernel { get; private set; }

        private Dictionary<int, Process> processes = new Dictionary<int, Process>();

        // TODO prevent "stealing" of pid by misbehaving apps (that take an id without registering. Probably just need to make NextPID private)
        public Stack<int> freedPIDs = new Stack<int>();
        private int nextPID = 1;
        // TODO make this a function probably for the above reason
        public int NextPID => freedPIDs.Count > 0 ? freedPIDs.Pop() : nextPID++;

        // TODO Maybe key this by the FileDescriptor rather than it's ID. That'll make it harder to accidentally steal FD from other processes through constructing FDs instead of loading them.
        public Dictionary<ulong, FileHandle> OpenFiles { get; } = new Dictionary<ulong, FileHandle>();
        public Dictionary<ulong, Process> OpenFileProcesses { get; } = new Dictionary<ulong, Process>();

        private Dictionary<int, int> parents = new Dictionary<int, int>();

        private Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();
        private Process initProcess;

        public Node()
        {
            Kernel = new Kernel(this);
        }

        internal Credentials Login(Process process, string username, string password)
        {
            Filesystem.Error error = Filesystem.Error.None;
            GameClient client = GetClient(process);

            // We use the init process for File access.

            FileDescriptor usersFile = Open(initProcess, "/etc/passwd", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                return null;
            }
            FileDescriptor groupFile = Open(initProcess, "/etc/group", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                return null;
            }

            byte[] passwdbytes = GetContent(usersFile, ref error);
            string passwdStr = Encoding.UTF8.GetString(passwdbytes);

            byte[] groupbytes = GetContent(groupFile, ref error);
            string groupStr = Encoding.UTF8.GetString(groupbytes);

            string[] accounts = passwdStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUsername = accountData[0];
                string accountPassword = accountData[1];
                string accountGroupId = accountData[3];

                if (accountUsername == username && accountPassword == password)
                {
                    Group primaryGroup = PermissionHelper.GetGroupFromString(accountGroupId);
                    if (primaryGroup == Group.INVALID)
                    {
                        client.Send(NetUtil.PacketType.MESSG, $"Can't login as {username}, '{accountGroupId}' is not a valid accountGroupId");
                        break;
                    }
                    List<Group> loginGroups = new List<Group>();
                    foreach (string group in groups)
                    {
                        string[] groupData = group.Split(':');
                        string groupName = groupData[0];
                        string groupId = groupData[2];
                        string[] groupUsers = groupData[3].Split(',');
                        if (groupUsers.Contains(username) || accountGroupId.Equals(groupId))
                        {
                            Group loginGroup = PermissionHelper.GetGroupFromString(groupId);
                            if (loginGroup != Group.INVALID)
                            {
                                loginGroups.Add(loginGroup);
                            }
                            else
                            {
                                client.Send(NetUtil.PacketType.MESSG, $"Can't login as {username} {groupName} is not a valid group");
                                break;
                            }
                        }
                    }
                    int userid = GetUserId(username);
                    if(error != Filesystem.Error.None)
                    {
                        Logger.Error($"No Such User, {username} when finding userid");
                        return null;
                    }
                    return new Credentials(userid, primaryGroup, loginGroups);
                }
            }
            return null;
        }

        internal void ReconnectClient(Process process, string host)
        {
            throw new NotImplementedException();
        }

        public void Connect(Process process, string host)
        {
            GameClient client = GetClient(process);
            if (client != null)
            {
                if (client.ActiveSession != null)
                    client.ActiveSession.DisconnectSession();
                // TODO create device file linked to print on connect and destroy on disconnect.
                // TODO set perms for device to client who logged in
                // TODO WHO Owns tty after SU ?!?!?! is it transfered to new user or does it belong to the original ?
                if (client.ActiveProcessSession != null)
                    client.ActiveProcessSession.DisconnectSession();
                Node connectingToNode = GetNodeByHost(client, host);
                if (connectingToNode != null)
                    client.ConnectTo(connectingToNode);
                else
                    client.Send(NetUtil.PacketType.KERNL, "connect", "fail", "0");
            }
        }

        internal string GetUsername(int userId)
        {
            Filesystem.Error error = Filesystem.Error.None;

            FileDescriptor usersFile = Open(initProcess, "/etc/passwd", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                return null;
            }
            FileDescriptor groupFile = Open(initProcess, "/etc/group", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                return null;
            }

            byte[] passwdbytes = GetContent(usersFile, ref error);
            string passwdStr = Encoding.UTF8.GetString(passwdbytes);

            byte[] groupbytes = GetContent(groupFile, ref error);
            string groupStr = Encoding.UTF8.GetString(groupbytes);

            string[] accounts = passwdStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUsername = accountData[0];
                string accountUserIdString = accountData[2];

                if (userId.ToString() == accountUserIdString)
                {
                    return accountUsername;
                }
            }
            return "";
        }

        private Node GetNodeByHost(GameClient client, string host)
        {
            var compManager = client.server.GetComputerManager();
            string resultIP = null;

            if (client.homeComputer != null)
            {
                if (host == "localhost" || host == "127.0.0.1")
                    resultIP = client.homeComputer.ip;
                else
                {
                    Node clientHome = client.homeComputer;
                    Filesystem clientFS = clientHome.Filesystems[clientHome.GetRootFilesystemId()];
                    // WARNING. this file handle originates on another computer. It cannot be used locally
                    FileHandle DNSConfigFile = clientFS.GetFileHandle("/cfg/dns.cfg");

                    if (DNSConfigFile != null)
                    {
                        Filesystem.Error error = Filesystem.Error.None;
                        int length = clientFS.GetFileLength(DNSConfigFile, ref error);
                        if (error != Filesystem.Error.None)
                            return null;

                        byte[] bytes = new byte[length];
                        clientFS.ReadFile(DNSConfigFile, bytes, 0, 0, length, ref error);
                        if (error != Filesystem.Error.None)
                            return null;

                        string content = Encoding.UTF8.GetString(bytes);

                        foreach (string ip in content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var DNSNode = compManager.GetNodeByIp(ip);
                            if (DNSNode == null)
                                continue;
                            var daemon = (DNSDaemon)DNSNode.GetDaemon("dns");
                            if (daemon == null)
                                continue;
                            resultIP = daemon.LookUp(host);
                            if (resultIP != null)
                                break;
                        }
                    }
                }
            }
            var connectingToNode = compManager.GetNodeByIp(resultIP ?? host);
            return connectingToNode;
        }

        public int GetUserId(string username)
        {
            Filesystem.Error error = Filesystem.Error.None;

            FileDescriptor usersFile = Open(initProcess, "/etc/passwd", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                return -1;
            }
            FileDescriptor groupFile = Open(initProcess, "/etc/group", FileDescriptor.Flags.Read, ref error);
            if (error != Filesystem.Error.None)
            {
                return -1;
            }

            byte[] passwdbytes = GetContent(usersFile, ref error);
            string passwdStr = Encoding.UTF8.GetString(passwdbytes);

            byte[] groupbytes = GetContent(groupFile, ref error);
            string groupStr = Encoding.UTF8.GetString(groupbytes);

            string[] accounts = passwdStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUsername = accountData[0];
                string accountUserIdString = accountData[2];

                if (accountUsername == username)
                {
                    return int.TryParse(accountUserIdString, out int result) ? result : -1;
                }
            }
            return -1;
        }


        public byte[] GetContent(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            int length = GetLength(fileDescriptor, ref error);
            if (error != Filesystem.Error.None)
            {
                return null;
            }
            byte[] buffer = new byte[length];
            ReadFile(fileDescriptor, buffer, 0, 0, length, ref error);
            return buffer;
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
            deviceSystem = new DeviceFileSystem(id, (ulong)Filesystems.Count);
            Filesystems[deviceSystem.ID] = deviceSystem;
            tempFileSystem = new TempFileSystem(id, (ulong)Filesystems.Count);
            Filesystems[tempFileSystem.ID] = tempFileSystem;
            pipeFileSystem = new TempFileSystem(id, (ulong)Filesystems.Count);
            Filesystems[pipeFileSystem.ID] = pipeFileSystem;

            tempFileSystem.RegisterNewFile(new TempInode(tempFileSystem, 1, DBUtil.GenerateMode(FileType.Directory, Permission.A_All)));
            Filesystems[0].LinkFile(Filesystems[0].GetFileHandle("/"), "tmp", tempFileSystem.ID, 1);
            TempInode dev = new TempInode(tempFileSystem, 2, DBUtil.GenerateMode(FileType.Directory, Permission.O_All | Permission.A_Execute | Permission.A_Read))
            {
                OwnerId = 0,
                Group = Group.ROOT
            };
            tempFileSystem.RegisterNewFile(dev);
            Filesystems[0].LinkFile(Filesystems[0].GetFileHandle("/"), "dev", tempFileSystem.ID, 2);

            initProcess = new Init(1, this, new Credentials(0, Group.ROOT));
            Filesystem.Error error = Filesystem.Error.None;
            initProcess.Init(ref error);

            if(error != Filesystem.Error.None)
            {
                Logger.Error("ERROR: {0} whilst init on processes", error);
            }

            initProcess.Run("");
        }

        public int GetLength(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            error = Filesystem.Error.None;
            long length;
            if(!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                length = -1;
            }
            else
            {
                // TODO int length files
                length = Filesystems[handle.FilesystemId].GetFileLength(handle, ref error);
            }
            return (int) length;
        }

        public void SetFileLength(FileDescriptor fileDescriptor, int length, ref Filesystem.Error error)
        {
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
            }
            else
            {
                Filesystems[handle.FilesystemId].SetFileLength(handle, length, ref error);
            }
        }

        internal File Mkdir(File activeDirectory, string name, Permission permission, ref Filesystem.Error error)
        {
            throw new NotImplementedException();
        }

        public int ReadFile(FileDescriptor fileDescriptor, byte[] buffer, int offset, int position, int count, ref Filesystem.Error error)
        {
            FileHandle handle;

            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return -1;
            }
            else
            {
                return Filesystems[handle.FilesystemId].ReadFile(handle, buffer, offset, position, count, ref error);
            }
        }

        public void WriteFile(FileDescriptor fileDescriptor, byte[] inputBuffer, int offset, int position, int count, ref Filesystem.Error error)
        {
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
            }
            else
            {
                Filesystems[handle.FilesystemId].WriteFile(handle, inputBuffer, offset, position, count, ref error);
            }
        }

        public FileDescriptor Open(Process process, string path, FileDescriptor.Flags flags, ref Filesystem.Error error)
        {
            return Open(process, path, flags, Permission.None, ref error);
        }

        public FileDescriptor Open(Process process, string pathStr, FileDescriptor.Flags flags, Permission mode, ref Filesystem.Error error)
        {
            FileHandle handle = Filesystems[GetRootFilesystemId()].GetFileHandle("/");
            return OpenAt(process, handle, pathStr, flags, mode, ref error);
        }


        public FileDescriptor OpenAt(FileDescriptor parent, string pathStr, FileDescriptor.Flags flags, Permission mode, ref Filesystem.Error error)
        {
            error = Filesystem.Error.None;

            List<string> path = new List<string>(pathStr.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));

            if (!OpenFiles.TryGetValue(parent.ID, out FileHandle parentHandle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return null;
            }

            Process parentProcess = OpenFileProcesses[parent.ID];

            return OpenAt(parentProcess, parentHandle, pathStr, flags, mode, ref error);
        }

        private FileDescriptor OpenAt(Process process, FileHandle atHandle, string pathStr, FileDescriptor.Flags flags, Permission mode, ref Filesystem.Error error)
        {
            error = Filesystem.Error.None;

            List<string> path = PathUtil.GetPath(pathStr);

            FileHandle handle = Filesystems[atHandle.FilesystemId].GetFileHandleAt(pathStr, atHandle);

            if(handle == null && (flags & FileDescriptor.Flags.Create_Open) == FileDescriptor.Flags.Create_Open)
            {
                string parentPath = PathUtil.Dirname(pathStr);
                FileHandle parentHandle = Filesystems[atHandle.FilesystemId].GetFileHandleAt(parentPath, atHandle);
                if(parentHandle != null)
                {
                    string fileName = PathUtil.Basename(path);
                    Credentials creds = process.Credentials;
                    handle = Filesystems[atHandle.FilesystemId].CreateFile(parentHandle, fileName, mode, creds.UserId, creds.Group, FileType.Regular);
                }
            }

            if (handle == null)
            {
                error = Filesystem.Error.No_Such_File;
                return null;
            }
            FileDescriptor fd = new FileDescriptor(handle.FilesystemId, (ulong)OpenFiles.Count, path);

            OpenFiles.Add(fd.ID, handle);
            OpenFileProcesses.Add(fd.ID, process);
            return fd;
        }

        internal void SetGroup(FileDescriptor fileDescriptor, Group value, ref Filesystem.Error error)
        {
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return;
            }
            Process process = OpenFileProcesses[fileDescriptor.ID];

            // Only root can "give away" files. This is not a bug. See https://unix.stackexchange.com/questions/27350/why-cant-a-normal-user-chown-a-file
            // BUT A user may give a file they own a group they belong.
            if ((process.Credentials.UserId == Filesystems[handle.FilesystemId].GetOwnerId(handle) && (process.Credentials.Group == value || process.Credentials.Groups.Contains(value))) || process.Credentials.UserId == 0)
            {
                Filesystems[handle.FilesystemId].SetGroup(handle, value);
            } else
            {
                error = Filesystem.Error.Permission_Denied;
            }
        }

        public void SetOwnerId(FileDescriptor fileDescriptor, int value, ref Filesystem.Error error)
        {
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return;
            }
            Process process = OpenFileProcesses[fileDescriptor.ID];
            // Only root can "give away" files. This is not a bug. See https://unix.stackexchange.com/questions/27350/why-cant-a-normal-user-chown-a-file
            if (process.Credentials.UserId != 0)
            {
                error = Filesystem.Error.Permission_Denied;
            }
            else
            {
                Filesystems[handle.FilesystemId].SetOwnerId(handle, value);
            }
        }

        public void SetPermissions(FileDescriptor fileDescriptor, Permission value, ref Filesystem.Error error)
        {
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return;
            }
            Process process = OpenFileProcesses[fileDescriptor.ID];
            // Only root can "give away" files. This is not a bug. See https://unix.stackexchange.com/questions/27350/why-cant-a-normal-user-chown-a-file
            if (process.Credentials.UserId != Filesystems[handle.FilesystemId].GetOwnerId(handle))
            {
                error = Filesystem.Error.Permission_Denied;
            }
            else
            {
                Filesystems[handle.FilesystemId].SetFilePermissions(handle, value);
            }
        }

        /// <summary>
        /// Returns a path only (no R/W) FileDescriptor for the parent of the given file.
        /// </summary>
        /// <param name="fileDescriptor"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public FileDescriptor GetParentFileDescriptor(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            error = Filesystem.Error.None;
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return null;
            }

            Process process = OpenFileProcesses[fileDescriptor.ID];

            return Open(process, PathUtil.Dirname(handle.FilePath.PathStr), 0, Permission.None, ref error);
        }

        internal int GetSessionId(int processId)
        {
            return SessionIds[processId];
        }

        public Process StartProcess(File file, Process parent)
        {
            FileDescriptor fileDescriptor = file.FileDescriptor;
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle) && file.HasExecutePermission(parent.Credentials))
            {
                uint checksum = Filesystems[handle.FilesystemId].GetFileChecksum(handle);

                string type = GetClient(parent).server.GetCompileManager().GetType(checksum);
                return CreateProcess(type, parent);
            }
            return null;
        }

        public uint? GetChecksum(FileDescriptor fileDescriptor)
        {
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                uint checksum = Filesystems[handle.FilesystemId].GetFileChecksum(handle);
                return checksum;
            }
            return null;
        }

        private Process CreateProcess(string type, Process parent)
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

            Filesystem.Error error = Filesystem.Error.None;
            process.Init(ref error);
            if (error != Filesystem.Error.None)
            {
                Logger.Error("ERROR: {0} whilst init on processes", error);
            }
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
            Filesystem.Error error = Filesystem.Error.None;
            var cfgFile = Kernel.GetFile(initProcess, SERVER_CONFIG_PATH, FileDescriptor.Flags.Read, ref error);
            if (cfgFile == null || error != Filesystem.Error.None)
                return ip;
            var lines = cfgFile.GetContentString().Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
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

        internal void Link(FileDescriptor activeDirectory, FileDescriptor target, string name, ref Filesystem.Error error)
        {
            if (!OpenFiles.TryGetValue(activeDirectory.ID, out FileHandle dirHandle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return;
            }
            if (!OpenFiles.TryGetValue(activeDirectory.ID, out FileHandle targHandle))
            {
                error = Filesystem.Error.Invalid_File_Descriptor;
                return;
            }
            Process process = OpenFileProcesses[activeDirectory.ID];

            if (CheckPermission(activeDirectory, Permission.A_Write, process.Credentials.UserId, process.Credentials.Group, process.Credentials.Groups))
            {
                Filesystems[dirHandle.FilesystemId].LinkFile(dirHandle, targHandle, name);
            }
            else
            {
                error = Filesystem.Error.Permission_Denied;
            }
        }

        internal void Unlink(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                Process process = OpenFileProcesses[fileDescriptor.ID];
                FileDescriptor parentFile = Open(process, string.Join("/", fileDescriptor.FilePath.Path), FileDescriptor.Flags.Write, ref error);
                if (error == Filesystem.Error.None && OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle parentHandle))
                {
                    if (CheckPermission(parentFile, Permission.A_Write, process.Credentials.UserId, process.Credentials.Group, process.Credentials.Groups))
                    {
                        Filesystems[parentHandle.FilesystemId].UnlinkFile(parentHandle, handle);
                    }
                    else
                    {
                        error = Filesystem.Error.Permission_Denied;
                    }
                }
            }
        }

        /// <summary>
        /// Check if a user can perform the given operation on this file.
        /// </summary>
        /// <param name="fileDescriptor"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        /// <param name="userGroup"></param>
        /// <param name="groups"></param>
        /// <returns>True if the user can perform the operation.</returns>
        public bool CheckPermission(FileDescriptor fileDescriptor, Permission value, int userId, Group userGroup, params Group[] groups)
        {
            List<Group> groupList = new List<Group>(groups)
            {
                userGroup
            };
            return CheckPermission(fileDescriptor, value, userId, groupList.ToArray());
        }

        /// <summary>
        /// Check if a user can perform the given operation on this file.
        /// </summary>
        /// <param name="fileDescriptor"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        /// <param name="groups"></param>
        /// <returns>True if the user can perform the operation.</returns>
        /// 
        public bool CheckPermission(FileDescriptor fileDescriptor, Permission value, int userId, params Group[] groups)
        {
            if (!OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                return false;
            }
            
            Permission filePerm = Filesystems[handle.FilesystemId].GetPermissions(handle);
            int fileOwnerId = Filesystems[handle.FilesystemId].GetOwnerId(handle);
            Group fileGroup = Filesystems[handle.FilesystemId].GetGroup(handle);
            // This will return true if any of the "types" of user pass.
            return PermissionHelper.CheckPermission((Permission)value & Permission.U_All, filePerm, fileOwnerId, fileGroup, userId, groups) || PermissionHelper.CheckPermission((Permission)value & Permission.G_All, filePerm, fileOwnerId, fileGroup, userId, groups) || PermissionHelper.CheckPermission((Permission)value & Permission.O_All, filePerm, fileOwnerId, fileGroup, userId, groups);
        }

        public int GetOwner(FileDescriptor fileDescriptor)
        {
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                return Filesystems[handle.FilesystemId].GetOwnerId(handle);
            }
            return -1;
        }

        public FileType GetType(FileDescriptor fileDescriptor)
        {
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                return Filesystems[handle.FilesystemId].GetFileType(handle);
            }
            //TODO Invalid/missing type or file
            return FileType.Regular;
        }

        public Group GetGroup(FileDescriptor fileDescriptor)
        {
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                return Filesystems[handle.FilesystemId].GetGroup(handle);
            }
            //TODO Notify of Invalid/missing type or file
            return Group.INVALID;
        }

        public Permission GetPermissions(FileDescriptor fileDescriptor)
        {
            if (OpenFiles.TryGetValue(fileDescriptor.ID, out FileHandle handle))
            {
                return Filesystems[handle.FilesystemId].GetPermissions(handle);
            }
            //TODO Invalid/missing type or file
            return Permission.None;
        }
    }
}
