using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Daemons;
using HackLinks_Server.Computers.Processes.Daemons;
using HackLinks_Server.Files;
using HackLinksCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Util;
using static HackLinksCommon.NetUtil;

namespace HackLinks_Server.Computers
{
    public class Kernel
    {
        private Node node;

        public Kernel(Node node)
        {
            this.node = node;
        }

        public int GetFileOwnerId(FileDescriptor fileDescriptor)
        {
            return node.GetOwner(fileDescriptor);
        }

        internal Group GetFileGroup(FileDescriptor fileDescriptor)
        {
            return node.GetGroup(fileDescriptor);
        }

        internal FileType GetFileType(FileDescriptor fileDescriptor)
        {
            return node.GetType(fileDescriptor);
        }

        internal Permission GetPermissions(FileDescriptor fileDescriptor)
        {
            return node.GetPermissions(fileDescriptor);
        }

        public void SetPermission(FileDescriptor fileDescriptor, Permission value, ref Filesystem.Error error)
        {
            node.SetPermissions(fileDescriptor, value, ref error);
        }

        internal byte[] GetFileContent(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            return node.GetContent(fileDescriptor, ref error);
        }

        public void SetOwnerId(FileDescriptor fileDescriptor, int value, ref Filesystem.Error error)
        {
            node.SetOwnerId(fileDescriptor, value, ref error);
        }

        public void SetGroup(FileDescriptor fileDescriptor, Group value, ref Filesystem.Error error)
        {
            node.SetGroup(fileDescriptor, value, ref error);
        }

        public void Print(Process process, string input)
        {
            GetProcessSession(process).WriteOutput(process, input);
        }

        internal List<FileUtil.DirRecord> GetDirectoryList(FileDescriptor fileDescriptor)
        {
            //TODO permission checks?
            return node.getDirectoryList(fileDescriptor);
        }

        private GameClient GetClient(Process process)
        {
            return node.GetClient(process);
        }

        public string GetContent(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            byte[] bytes = new byte[node.GetLength(fileDescriptor, ref error)];
            if (error != Filesystem.Error.None)
            {
                return null;
            }

            node.ReadFile(fileDescriptor, bytes, 0, 0, bytes.Length, ref error);
            return Encoding.UTF8.GetString(bytes);
        }

        public void SetContent(FileDescriptor fileDescriptor, string newContent, ref Filesystem.Error error)
        {
            error = Filesystem.Error.None;

            byte[] bytes = Encoding.UTF8.GetBytes(newContent);
            node.WriteFile(fileDescriptor, bytes, 0, 0, bytes.Length, ref error);

            if (error != Filesystem.Error.None)
                return;

            // truncate the file to its new length.
            node.SetFileLength(fileDescriptor, bytes.Length, ref error);
        }

        public FileDescriptor GetParentFileDescriptor(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            return node.GetParentFileDescriptor(fileDescriptor, ref error);
        }

        public File GetParentFile(File file, ref Filesystem.Error error)
        {
            FileDescriptor parent = GetParentFileDescriptor(file.FileDescriptor, ref error);

            if (error != Filesystem.Error.None)
                return null;

            return new File(parent, this);
        }

        private ProcessSession GetProcessSession(Process process)
        {
            return node.GetProcessSession(process.ProcessId);
            // TODO throw exception if null. All processes here should belong to a session.
        }

        public void Display(Process process, string type, params string[] data)
        {
            List<string> completeData = new List<string>(){"state", type};
            completeData.AddRange(data);
            GetClient(process).Send(NetUtil.PacketType.KERNL, completeData.ToArray());
        }

        internal bool CheckPermission(FileDescriptor fileDescriptor, Permission value, int userId, Group[] privs)
        {
            return node.CheckPermission(fileDescriptor, value, userId, privs);
        }

        public uint? GetChecksum(FileDescriptor fileDescriptor)
        {
            return node.GetChecksum(fileDescriptor);
        }

        public void Login(CommandProcess process, string username, string password)
        {
            GameClient client = GetClient(process);
            Credentials credentials = node.Login(process, username, password);
            if (credentials != null)
            {
                client.Login(node, credentials);
                client.Send(NetUtil.PacketType.MESSG, "Logged as : " + username);
                // TODO log
                //Log(Log.LogEvents.Login, node.logs.Count + 1 + " " + client.homeComputer.ip + " logged in as " + username, client.ActiveSession.sessionId, client.homeComputer.ip);
            } else
            {
                client.Send(NetUtil.PacketType.MESSG, "Wrong identificants.");
            }
        }

        public void Connect(Process process, string host)
        {
            node.ReconnectClient(process, host);
        }

        internal FileDescriptor Open(Process process, string path, FileDescriptor.Flags flags, ref Filesystem.Error error)
        {
            return Open(process, path, flags, Permission.None, ref error);
        }

        /// <summary>
        /// If the given is currently an attached process, then reattach the parent instead
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public void ReattachParent(Process parent, Process child)
        {
            GameClient client = GetClient(child);
            if (client != null)
            {
                ProcessSession session = client.ActiveProcessSession;
                if (session.HasProcessId(child.ProcessId))
                {
                    session.AttachProcess(parent);
                }
            }
        }

        public void Disconnect(CommandProcess process)
        {
            GetClient(process).Disconnect();
        }

        public void LS(Process process, string[] fileData)
        {
            List<string> data = new List<string>() { "ls" };
            data.AddRange(fileData);

            GetClient(process).Send(NetUtil.PacketType.KERNL, data.ToArray());
        }

        public void CD(Process process, string name)
        {
            GameClient client = GetClient(process);
            if (client != null) {
                client.Send(NetUtil.PacketType.KERNL, new string[] { "cd", name });
            }
        }

        public Process StartProcess(Process process, File file)
        {
            return node.StartProcess(file, process);
        }

        public void PlayMusic(Process process, string song)
        {
            GetClient(process).Send(NetUtil.PacketType.MUSIC, song);
        }

        public void OpenDaemon(CommandProcess process, string target)
        {
            Session ActiveSession = GetClient(process).ActiveSession;
            foreach (Daemon daemon in ActiveSession.connectedNode.daemons)
            {
                if (daemon.IsOfType(target))
                {
                    DaemonClient daemonClient = daemon.CreateClient(ActiveSession, process);
                    node.SetupChildProcess(process, daemonClient);
                    daemon.OnConnect(ActiveSession, daemonClient);
                    GetClient(process).ActiveProcessSession.AttachProcess(daemonClient);
                }
            }
        }

        public List<Account> GetAccounts(Process process)
        {
            Filesystem.Error error = Filesystem.Error.None;
            File file = node.Kernel.GetFile(process, "/etc/passwd", FileDescriptor.Flags.Read, ref error);
            return error == Filesystem.Error.None ? Account.FromFile(process, file, node) : null;
        }

        public string GetUsername(int userId)
        {
            return node.GetUsername(userId);
        }

        public void Log(Log.LogEvents logEvent, string message, int sessionId, string ip)
        {
            //TODO fix logging
        //    File logsFolder = null;
        //    foreach (var file in fileSystem.rootFile.children)
        //    {
        //        if (file.Name == "logs")
        //        {
        //            logsFolder = file;
        //            break;
        //        }
        //    }
        //    if (logsFolder == null)
        //    {
        //        logsFolder = RegularFile.CreateNewFolder(fileSystem.fileSystemManager, this, fileSystem.rootFile, "logs");
        //        logsFolder.OwnerId = 0;
        //        logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
        //        logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
        //        logsFolder.Group = logsFolder.Parent.Group;
        //        logsFolder.Type = FileType.LOG;
        //    }
        //    message = message.Replace(' ', '_');
        //    File logFile = RegularFile.CreateNewFile(fileSystem.fileSystemManager, this, logsFolder, message);
        //    logFile.OwnerId = 0;
        //    logFile.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
        //    logFile.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
        //    logFile.Group = logsFolder.Parent.Group;
        //    logFile.Type = FileType.LOG;
        //    logs.Add(new Log(logFile, sessionId, ip, logEvent, message));
        //}

        //public void Log(Log.LogEvents logEvent, string message, string messageExtended, int sessionId, string ip)
        //{
        //    File logsFolder = null;
        //    foreach (var file in fileSystem.rootFile.children)
        //    {
        //        if (file.Name == "logs")
        //        {
        //            logsFolder = file;
        //            break;
        //        }
        //    }
        //    if (logsFolder == null)
        //    {
        //        logsFolder = RegularFile.CreateNewFolder(fileSystem.fileSystemManager, this, fileSystem.rootFile, "logs");
        //        logsFolder.OwnerId = 0;
        //        logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
        //        logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
        //        logsFolder.Group = logsFolder.Parent.Group;
        //        logsFolder.Type = FileType.LOG;
        //    }
        //    File logFile = RegularFile.CreateNewFile(fileSystem.fileSystemManager, this, logsFolder, message);
        //    logFile.OwnerId = 0;
        //    logFile.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
        //    logFile.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
        //    logFile.Group = logsFolder.Parent.Group;
        //    logFile.Type = FileType.LOG;
        //    logs.Add(new Log(logFile, sessionId, ip, logEvent, message, messageExtended));
        //}

        //internal void ParseLogs()
        //{
        //    Filesystems fileSystem = GetFileSystem(node.fi);
        //    List<Log> logs = new List<Log>();
        //    File logsFolder = null;
        //    foreach (var file in Kernel.children)
        //    {
        //        if (file.Name == "logs")
        //        {
        //            logsFolder = file;
        //            break;
        //        }
        //    }
        //    if (logsFolder == null)
        //        return;

        //    foreach (var log in logsFolder.Children)
        //    {
        //        string machineReadChars = "";
        //        int machineReadCharType = 0;
        //        int machineReadCharsFound = 0;
        //        int machineReadSplit = 0;
        //        foreach (var character in log.Content)
        //        {
        //            if (character == '#' && machineReadCharType == 0 && machineReadCharsFound < 4)
        //            {
        //                machineReadChars = machineReadChars + "#";
        //                machineReadCharsFound++;
        //                if (machineReadCharsFound >= 4)
        //                {
        //                    machineReadCharType++;
        //                    machineReadCharsFound = 0;
        //                }
        //            }
        //            else if (character == '!' && machineReadCharType == 1 && machineReadCharsFound < 2)
        //            {
        //                machineReadChars = machineReadChars + "!";
        //                machineReadCharsFound++;
        //                if (machineReadCharsFound >= 2)
        //                {
        //                    machineReadCharType++;
        //                    machineReadCharsFound = 0;
        //                }
        //            }
        //            else if (character == '*' && machineReadCharType == 2 && machineReadCharsFound < 1)
        //            {
        //                machineReadChars = machineReadChars + "*";
        //                machineReadCharsFound++;
        //            }
        //            else if (machineReadChars == "####!!*")
        //                break;
        //            else
        //            {
        //                machineReadChars = "";
        //                machineReadCharType = 0;
        //                machineReadCharsFound = 0;
        //            }
        //            machineReadSplit++;
        //        }

        //        machineReadSplit += 23;
        //        Log logAdd = Computers.Log.Deserialize(log.Content.Substring(machineReadSplit));
        //        logAdd.file = log;
        //        logs.Add(logAdd);
        //    }

        //    this.logs = logs;
        }


        public void LinkFile(FileDescriptor activeDirectory, FileDescriptor target, string name, ref Filesystem.Error error)
        {
            node.Link(activeDirectory, target, name, ref error);
        }

        public void UnlinkFile(FileDescriptor fileDescriptor, ref Filesystem.Error error)
        {
            node.Unlink(fileDescriptor, ref error);
        }

        public bool HasUser(string username)
        {
            return node.GetUserId(username) != -1;
        }

        public int GetUserId(string username)
        {
            return node.GetUserId(username);
        }

        public FileDescriptor Open(Process process, string path, FileDescriptor.Flags flags, Permission mode, ref Filesystem.Error error)
        {
            return node.Open(process, path, flags, mode, ref error);
        }

        public FileDescriptor OpenAt(FileDescriptor fileDescriptor, string path, FileDescriptor.Flags flags, Permission mode, ref Filesystem.Error error)
        {
            return node.OpenAt(fileDescriptor, path, flags, mode, ref error);
        }

        public File GetFile(Process process, string path, FileDescriptor.Flags flags, Permission permission, ref Filesystem.Error error)
        {
            FileDescriptor fileDescriptor = Open(process, path, flags, permission, ref error);

            if (error == Filesystem.Error.None && fileDescriptor != null)
            {
                return new File(fileDescriptor, this);
            }
            else
            {
                return null;
            }
        }

        public File GetFile(Process process, string path, FileDescriptor.Flags flags, ref Filesystem.Error error)
        {
            FileDescriptor fileDescriptor = Open(process, path, flags, Permission.None, ref error);

            if (error == Filesystem.Error.None && fileDescriptor != null)
            {
                return new File(fileDescriptor, this);
            }
            else
            {
                return null;
            }
        }

        public File GetFileAt(File file, string path, FileDescriptor.Flags flags, ref Filesystem.Error error)
        {
            return GetFileAt(file, path, flags, Permission.None, ref error);
        }

        public File GetFileAt(File file, string path, FileDescriptor.Flags flags, Permission mode, ref Filesystem.Error error)
        {
            FileDescriptor descriptor = OpenAt(file.FileDescriptor, path, flags, mode, ref error);

            if (error == Filesystem.Error.None && descriptor != null)
            {
                return new File(descriptor, this);
            }
            else
            {
                return null;
            }
        }

        internal File Mkdir(File activeDirectory, string name, Permission permission, ref Filesystem.Error error)
        {
            return node.Mkdir(activeDirectory, name, permission, ref error);
        }
    }
}
