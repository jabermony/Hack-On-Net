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

namespace HackLinks_Server.Computers
{
    public class Kernel
    {
        private Node node;

        public Kernel(Node node)
        {
            this.node = node;
        }

        public int GetFilePermissionValue(FileHandle fileHandle)
        {
            return node.Filesystems[fileHandle.FilesystemId].GetPermissions(fileHandle);
        }

        public bool SetFilePermissionValue(Process process, FileHandle fileHandle, int value)
        {
            if(process.Credentials.UserId == GetFileOwnerId(fileHandle) || process.Credentials.UserId == 0)
            {
                node.Filesystems[fileHandle.FilesystemId].SetFilePermissions(fileHandle, value);
                return true;
            }
            return false;
        }

        public int GetFileOwnerId(FileHandle fileHandle)
        {
            return node.Filesystems[fileHandle.FilesystemId].GetOwnerId(fileHandle);
        }

        public FileType GetFileType(FileHandle fileHandle)
        {
            return node.Filesystems[fileHandle.FilesystemId].GetFileType(fileHandle);
        }

        public bool SetFileOwnerId(Process process, FileHandle fileHandle, int value)
        {
            // Only root can "give away" files. This is not a bug. See https://unix.stackexchange.com/questions/27350/why-cant-a-normal-user-chown-a-file
            if (process.Credentials.UserId == 0)
            {
                node.Filesystems[fileHandle.FilesystemId].SetOwnerId(fileHandle, value);
                return true;
            }
            return false;
        }

        public Group GetFileGroup(FileHandle fileHandle)
        {
            return node.Filesystems[fileHandle.FilesystemId].GetGroup(fileHandle);
        }

        public void Print(Process process, string input)
        {
            GetClient(process).Send(PacketType.MESSG, input);
        }

        public bool SetFileGroup(Process process, FileHandle fileHandle, Group value)
        {
            // Only root can "give away" files. This is not a bug. See https://unix.stackexchange.com/questions/27350/why-cant-a-normal-user-chown-a-file
            // BUT A user may give a file they own a group they belong.
            if ((process.Credentials.UserId == GetFileOwnerId(fileHandle) && (process.Credentials.Group == value || process.Credentials.Groups.Contains(value))) || process.Credentials.UserId == 0)
            {
                node.Filesystems[fileHandle.FilesystemId].SetGroup(fileHandle, value);
                return true;
            }
            return false;
        }

        private GameClient GetClient(Process process)
        {
            Session session = node.GetSession(process.ProcessId) ?? null;
            return session  != null? session.owner : null;
        }

        public string GetFileContent(Process process, FileHandle fileHandle)
        {
            if(CheckPermission(fileHandle, PermissionClass.Read, process.Credentials.UserId, process.Credentials.Group, process.Credentials.Groups))
            {
                return GetFileContent(fileHandle);
            }
            return null;
        }

        private string GetFileContent(FileHandle fileHandle)
        {
            // TODO optimise
            System.IO.Stream fStream = node.Filesystems[fileHandle.FilesystemId].GetFileContent(fileHandle);
            byte[] bytes = new byte[fStream.Length];
            fStream.Read(bytes, 0, (int)fStream.Length);
            return Encoding.UTF8.GetString(bytes);
        }

        public bool SetFileContent(Process process, FileHandle fileHandle, string newContent)
        {
            if (CheckPermission(fileHandle, PermissionClass.Write, process.Credentials.UserId, process.Credentials.Group, process.Credentials.Groups))
            {
                SetFileContent(fileHandle, newContent);
                return true;
            }
            return false;
        }

        private void SetFileContent(FileHandle fileHandle, string newContent)
        {
            // TODO optimise
            System.IO.Stream fStream = node.Filesystems[fileHandle.FilesystemId].GetFileContent(fileHandle);
            byte[] bytes = Encoding.UTF8.GetBytes(newContent);
            fStream.Write(bytes, 0, bytes.Length);
            // truncate the file to its new length.
            fStream.SetLength(bytes.Length);
        }

        public uint GetFileChecksum(FileHandle fileHandle)
        {
            return node.Filesystems[fileHandle.FilesystemId].GetFileChecksum(fileHandle);
        }

        public List<File> GetChildren(Process process, File file)
        {
            if (CheckPermission(file.FileHandle, PermissionClass.Execute, process.Credentials.UserId, process.Credentials.Group, process.Credentials.Groups))
            {
                Filesystem fileSystem = GetFileSystem(file.FileHandle.FilesystemId);
                return FileUtil.GetDirectoryListAsFiles(process, fileSystem, file);
            }
            return new List<File>();
        }

        public FileHandle GetParentFileHandle(FileHandle fileHandle)
        {
            return fileHandle.FilePath.Parent;
        }

        public File GetParentFile(Process process, File file)
        {
            return new File(process, file.FileHandle.FilePath.Parent);
        }

        private Session GetSession(Process process)
        {
            return node.GetSession(process.ProcessId);
            // TODO throw exception if null. All processes here should belong to a session.
        }

        public void Display(Process process, string type, params string[] data)
        {
            List<string> completeData = new List<string>(){"state", type};
            completeData.AddRange(data);
            GetClient(process).Send(NetUtil.PacketType.KERNL, completeData.ToArray());
        }

        public void Login(CommandProcess process, string username, string password)
        {
            GameClient client = GetClient(process);
            Credentials credentials = Login(GetClient(process), username, password);
            if (credentials != null)
            {
                client.Login(node, credentials);
                client.Send(NetUtil.PacketType.MESSG, "Logged as : " + username);
                // TODO log
                //Log(Log.LogEvents.Login, node.logs.Count + 1 + " " + client.homeComputer.ip + " logged in as " + username, client.activeSession.sessionId, client.homeComputer.ip);
            } else
            {
                client.Send(NetUtil.PacketType.MESSG, "Wrong identificants.");
            }
        }

        private Credentials Login(GameClient client, string username, string password)
        {
            FileHandle usersFile = GetFileHandle("/etc/passwd");
            if (usersFile == null)
            {
                client.Send(NetUtil.PacketType.MESSG, "No passwd file was found!");
                return null;
            }
            FileHandle groupFile = GetFileHandle("/etc/group");
            if (usersFile == null)
            {
                client.Send(NetUtil.PacketType.MESSG, "No group file was found!");
                return null;
            }
            string[] accounts = GetFileContent(usersFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = GetFileContent(groupFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

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
                    return new Credentials(GetUserId(username), primaryGroup, loginGroups);
                }
            }
            return null;
        }

        /// <summary>
        /// Check if a user can perform the given operation on this file.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        /// <param name="userGroup"></param>
        /// <param name="groups"></param>
        /// <returns>True if the user can perform the operation.</returns>
        public bool CheckPermission(FileHandle fileHandle, PermissionClass value, int userId, Group userGroup, params Group[] groups)
        {
            List<Group> groupList = new List<Group>(groups)
            {
                userGroup
            };
            // TODO FIX got to do a check on each "class" instead of a widespread check. Else we'll not get anywhere
            return CheckPermission(fileHandle, value, userId, groupList.ToArray());
        }
        /// <summary>
        /// Check if a user can perform the given operation on this file.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        /// <param name="groups"></param>
        /// <returns>True if the user can perform the operation.</returns>
        public bool CheckPermission(FileHandle fileHandle, PermissionClass value, int userId, params Group[] groups)
        {
            int filePerm = node.Filesystems[fileHandle.FilesystemId].GetPermissions(fileHandle);
            int fileOwnerId = node.Filesystems[fileHandle.FilesystemId].GetOwnerId(fileHandle);
            Group fileGroup = node.Filesystems[fileHandle.FilesystemId].GetGroup(fileHandle);
            // This will return true if any of the "types" of user pass.
            return PermissionHelper.CheckPermission((Permission)value & Permission.U_All, filePerm, fileOwnerId, fileGroup, userId, groups) || PermissionHelper.CheckPermission((Permission)value & Permission.G_All, filePerm, fileOwnerId, fileGroup, userId, groups) || PermissionHelper.CheckPermission((Permission)value & Permission.O_All, filePerm, fileOwnerId, fileGroup, userId, groups);
        }

        private Filesystem GetFileSystem(ulong id)
        {
            return node.Filesystems[id];
        }

        public void Connect(Process process, string host)
        {
            GameClient client = GetClient(process);
            if (client.activeSession != null)
                client.activeSession.DisconnectSession();
            var compManager = client.server.GetComputerManager();
            string resultIP = null;

            if (client.homeComputer != null)
            {
                if (host == "localhost" || host == "127.0.0.1")
                    resultIP = client.homeComputer.ip;
                else
                {
                    // WARNING. this file handle originates on another computer. It cannot be used locally
                    FileHandle DNSConfigFile = client.homeComputer.Kernel.GetFileHandle("/cfg/dns.cfg");
                    if (DNSConfigFile != null)
                    {
                        foreach (string ip in client.homeComputer.Kernel.GetFileContent(DNSConfigFile).Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
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
            if (connectingToNode != null)
                client.ConnectTo(connectingToNode);
            else
                client.Send(NetUtil.PacketType.KERNL, "connect", "fail", "0");
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
                Session session = client.activeSession;
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
            GetClient(process).Send(NetUtil.PacketType.KERNL, new string[] { "cd", name});
        }

        public Process StartProcess(Process process, File file)
        {
            if(!file.HasExecutePermission(process.Credentials))
                StartProcess(process, "False");
            string type = GetClient(process).server.GetCompileManager().GetType(file.Checksum);
            return StartProcess(process, type);
        }

        private Process StartProcess(Process process, string type)
        {
            Process child = GetClient(process).CreateProcess(node, type, process);
            StartProcess(process, child);
            return child;
        }

        public Process StartProcess(Process process, Process child)
        {
            node.SetChildProcess(process, child);
            child.ActiveDirectory = process.ActiveDirectory;
            return child;
        }

        public void PlayMusic(Process process, string song)
        {
            GetClient(process).Send(NetUtil.PacketType.MUSIC, song);
        }

        public void OpenDaemon(CommandProcess process, string target)
        {
            Session activeSession = GetClient(process).activeSession;
            foreach (Daemon daemon in activeSession.connectedNode.daemons)
            {
                if (daemon.IsOfType(target))
                {
                    DaemonClient daemonClient = daemon.CreateClient(activeSession, process);
                    StartProcess(process, daemonClient);
                    daemon.OnConnect(activeSession, daemonClient);
                    GetClient(process).activeSession.AttachProcess(daemonClient);
                }
            }
        }

        public List<Account> GetAccounts(Process process)
        {
            return Account.FromFile(process, node.Kernel.GetFile(process, "/etc/passwd"), node);
        }

        public string GetUsername(int userId)
        {
            FileHandle usersFile = GetFileHandle(node.GetRootFilesystemId(), "passwd");
            if (usersFile == null)
            {
                return "";
            }
            FileHandle groupFile = GetFileHandle(node.GetRootFilesystemId(), "/etc/group");
            if (usersFile == null)
            {
                return "";
            }
            string[] accounts = GetFileContent(usersFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = GetFileContent(groupFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

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

        public int GetUserId(string username)
        {
            FileHandle usersFile = GetFileHandle(node.GetRootFilesystemId(), "/etc/passwd");
            if (usersFile == null)
            {
                return -1;
            }
            FileHandle groupFile = GetFileHandle(node.GetRootFilesystemId(), "/etc/group");
            if (usersFile == null)
            {
                return -1;
            }
            string[] accounts = GetFileContent(usersFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = GetFileContent(groupFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

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

        public void UnlinkFile(Process process, FileHandle fileHandle)
        {
            if(CheckPermission(fileHandle, PermissionClass.Write, process.Credentials.UserId, process.Credentials.Group, process.Credentials.Groups))
            {
                GetFileSystem(fileHandle.FilesystemId).UnlinkFile(fileHandle);
            }
        }

        public FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group, FileType type)
        {
            return GetFileSystem(directory.FilesystemId).CreateFile(directory, name, permissions, ownerId, group, type);
        }

        public FileHandle CreateFile(FileHandle directory, string name, Permission permissions, int ownerId, Group group)
        {
            return CreateFile(directory, name, permissions, ownerId, group, FileType.Regular);
        }

        public File CreateFile(Process process, File directory, string name, Permission permissions, int ownerId, Group group, FileType type)
        {
            return new File(process, CreateFile(directory.FileHandle, name, permissions, ownerId, group, type));
        }

        public File CreateFile(Process process, File directory, string name, Permission permissions, int ownerId, Group group)
        {
            return CreateFile(process, directory, name, permissions, ownerId, group, FileType.Regular);
        }

        public bool HasUser(string username)
        {
            return GetUserId(username) != -1;
        }

        public string GetUserShell(int userId)
        {
            FileHandle usersFile = GetFileHandle(node.GetRootFilesystemId(), "passwd");
            if (usersFile == null)
            {
                return "";
            }
            FileHandle groupFile = GetFileHandle(node.GetRootFilesystemId(), "/etc/group");
            if (usersFile == null)
            {
                return "";
            }
            string[] accounts = GetFileContent(usersFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = GetFileContent(groupFile).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUserIdString = accountData[2];

                if (userId.ToString() == accountUserIdString)
                {
                    string accountUserShell = accountData[6];
                    return accountUserShell;
                }
            }
            return "";
        }
        
        public File GetFile(Process process, string path)
        {
            return GetFile(process, node.GetRootFilesystemId(), path);
        }

        public FileHandle GetFileHandle(string path)
        {
            return GetFileHandle(node.GetRootFilesystemId(), path);
        }

        public FileHandle GetFileHandle(ulong filesystemId, string path)
        {
            Filesystem fileSystem = GetFileSystem(filesystemId);
            return fileSystem.GetFileHandle(path);
        }

        public File GetFile(Process process, ulong filesystemId, string path)
        {
            FileHandle handle = GetFileHandle(filesystemId, path);
            if(handle != null)
            {
                return new File(process, handle);
            }
            else
            {
                return null;
            }
        }


        public File GetFileAt(Process process, File file, string name)
        {
            FileHandle handle = GetFileHandleAt(file.FileHandle, name);
            if (handle != null)
            {
                return new File(process, handle);
            }
            else
            {
                return null;
            }
        }

        private FileHandle GetFileHandleAt(FileHandle fileHandle, string name)
        {
            Filesystem fileSystem = GetFileSystem(fileHandle.FilesystemId);
            return fileSystem.GetFileHandleAt(name, fileHandle);
        }
    }
}
