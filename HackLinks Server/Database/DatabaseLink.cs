using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Util;
using HackLinks_Server.Computers.Filesystems;
using System.Data;

namespace HackLinks_Server.Database
{
    public class DatabaseLink
    {
        private MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder();

        public DatabaseLink(ConfigUtil.ConfigData config)
        {
            SetConnectionStringParameters(config);
        }

        public void SetConnectionStringParameters(ConfigUtil.ConfigData config)
        {
            connectionStringBuilder.Server = config.MySQLServer;
            connectionStringBuilder.Database = config.Database;
            connectionStringBuilder.UserID = config.UserID;
            connectionStringBuilder.Password = config.Password;
        }

        public void SetFileLength(int computerId, ulong filesystemId, ulong inode, long newLength)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();                
                MySqlCommand fileCommand = new MySqlCommand("UPDATE files SET content = RPAD(SUBSTRING(content, @position, @count), @count, CHAR(0)) WHERE computer_Id = @computer_Id AND filesystem_id = @filesystem_id AND inode = @inode", conn);
                fileCommand.Parameters.AddRange(
                    new MySqlParameter[]{
                        new MySqlParameter("position", 0),
                        new MySqlParameter("count", newLength),
                        new MySqlParameter("computer_Id", computerId),
                        new MySqlParameter("filesystem_id", filesystemId),
                        new MySqlParameter("inode", inode)
                    }
                );
                fileCommand.ExecuteNonQuery();
                // TODO check for failure ?
            }
        }

        public void CreateFile(int computerId, ulong filesystemId, ulong inode, int linkCount, int mode, int groupId, int ownerId, byte[] content)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand fileCommand = new MySqlCommand(
                "INSERT INTO files (computer_Id, filesystem_id, inode, link_count, mode, group_id, user_id, content) " +
                "VALUES (@computer_id, @filesystem_id, @inode, @link_count, @mode, @group_id, @user_id, @content) "
                , conn);
                fileCommand.Parameters.AddRange(
                    new MySqlParameter[]{
                        new MySqlParameter("computer_Id", computerId),
                        new MySqlParameter("filesystem_id", filesystemId),
                        new MySqlParameter("inode", inode),
                        new MySqlParameter("link_count", linkCount),
                        new MySqlParameter("mode", mode),
                        new MySqlParameter("group_id", groupId),
                        new MySqlParameter("user_id", ownerId),
                        new MySqlParameter("content", content)
                    }
                );
                fileCommand.ExecuteNonQuery();
                // TODO check for failure ?
            }
        }
        

        /// <summary>
        /// Write buffer bytes to the given file.
        /// </summary>
        /// <param name="computerId"></param>
        /// <param name="filesystemId"></param>
        /// <param name="inode"></param>
        /// <param name="buffer"></param>
        /// <param name="position">the position within the file to begin writing from</param>
        /// <returns>Number of bytes read</returns>
        public void WriteFile(int computerId, ulong filesystemId, ulong inode, byte[] buffer, long position)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand fileCommand = new MySqlCommand("UPDATE files SET content = INSERT(RPAD(content, @minLen, CHAR(0)), @position, @count, @buffer) WHERE computer_Id = @computer_Id AND filesystem_id = @filesystem_id AND inode = @inode", conn);
                fileCommand.Parameters.AddRange(
                    new MySqlParameter[]{
                        new MySqlParameter("position", position + 1),
                        new MySqlParameter("count", buffer.Length),
                        new MySqlParameter("computer_Id", computerId),
                        new MySqlParameter("filesystem_id", filesystemId),
                        new MySqlParameter("inode", inode),
                        new MySqlParameter("buffer", buffer),
                        new MySqlParameter("minLen", position + buffer.Length)
                    }
                );
                var b = fileCommand.ExecuteNonQuery();
                // TODO check for failure ?
            }
        }

        /// <summary>
        /// Read a stream of bytes from the given file into the provided array buffer.
        /// </summary>
        /// <param name="computerId"></param>
        /// <param name="filesystemId"></param>
        /// <param name="inode"></param>
        /// <param name="buffer"></param>
        /// <param name="offset">offset within the array to begin reading to</param>
        /// <param name="position">the position within the file to begin reading from</param>
        /// <param name="count">the maximum number of bytes to read</param>
        /// <returns>Number of bytes read</returns>
        public long ReadFile(int computerId, ulong filesystemId, ulong inode, byte[] buffer, int offset, long position, int count)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand fileCommand = new MySqlCommand("SELECT SUBSTRING(content, @position, @count) FROM files WHERE computer_Id = @computer_Id AND filesystem_id = @filesystem_id AND inode = @inode", conn);
                fileCommand.Parameters.AddRange(
                    new MySqlParameter[]{
                        new MySqlParameter("position", position + 1),
                        new MySqlParameter("count", count),
                        new MySqlParameter("computer_Id", computerId),
                        new MySqlParameter("filesystem_id", filesystemId),
                        new MySqlParameter("inode", inode)
                    }
                );
                using (MySqlDataReader fileReader = fileCommand.ExecuteReader())
                {
                    if(fileReader.HasRows)
                    {
                        fileReader.Read();
                        return fileReader.GetBytes(0, 0, buffer, offset, count);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        /// <summary>
        /// Read a stream of bytes from the given file into the provided array buffer.
        /// </summary>
        /// <param name="computerId"></param>
        /// <param name="filesystemId"></param>
        /// <param name="inode"></param>
        /// <returns>Number of bytes in file</returns>
        public long GetFileLength(int computerId, ulong filesystemId, ulong inode)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand fileCommand = new MySqlCommand("SELECT LENGTH(content) FROM files WHERE computer_Id = @computer_Id AND filesystem_id = @filesystem_id AND inode = @inode", conn);
                fileCommand.Parameters.AddRange(
                    new MySqlParameter[]{
                        new MySqlParameter("computer_Id", computerId),
                        new MySqlParameter("filesystem_id", filesystemId),
                        new MySqlParameter("inode", inode)
                    }
                );
                using (MySqlDataReader fileReader = fileCommand.ExecuteReader())
                {
                    if (fileReader.HasRows)
                    {
                        fileReader.Read();
                        if(!fileReader.IsDBNull(0))
                        {
                            return fileReader.GetInt64(0);
                        }
                    }
                    return 0;
                }
            }
        }

        public string GetConnectionString()
        {         
            return connectionStringBuilder.GetConnectionString(true);
        }

        public List<Node> DownloadDatabase()
        {
            List<Node> nodeList = new List<Node>();

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();

                MySqlCommand sqlCommand = new MySqlCommand("SELECT * FROM computers", conn);
                using (MySqlConnection cn1 = new MySqlConnection(GetConnectionString()))
                {
                    cn1.Open();
                    using (MySqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Node newNode = new Node
                                {
                                    id = reader.GetInt32(0),
                                    ip = reader.GetString(1),
                                    ownerId = reader.GetInt32(2)
                                };

                                Logger.Info($"Creating Node {newNode.id} with ip {newNode.ip}");

                                MySqlCommand fileCommand = new MySqlCommand("SELECT computer_id, filesystem_id, inode, link_count, mode, group_id, user_id, inode_change_time, last_modified_time, last_accessed_time, content FROM files WHERE computer_Id = @computer_Id AND filesystem_id = 0", cn1);
                                fileCommand.Parameters.Add(new MySqlParameter("computer_Id", newNode.id));
                                List<DiskInode> computerInodes = new List<DiskInode>();

                                DiskFileSystem fileSystem = new DiskFileSystem(newNode.id, 0);

                                using (MySqlDataReader fileReader = fileCommand.ExecuteReader())
                                {
                                    if (fileReader.HasRows)
                                    {
                                        while (fileReader.Read())
                                        {
                                            ulong fileId = fileReader.GetUInt64("inode");
                                            int mode = fileReader.GetInt32("mode");
                                            Logger.Info($"Creating {(FileType)(mode >> 9 & 0b111)} with id {fileId}");

                                            DiskInode newFile = new DiskInode(fileSystem, fileId, fileReader.GetInt32("mode"))
                                            {
                                                Group = (Group)fileReader.GetInt32("group_id"),
                                                OwnerId = fileReader.GetInt32("user_id")
                                            };

                                            computerInodes.Add(newFile);
                                        }
                                    }
                                }
                                fileSystem.RegisterNewFiles(computerInodes);
                                newNode.Filesystems[fileSystem.ID] = fileSystem;
                                //newNode.ParseLogs();
                                nodeList.Add(newNode);
                            }
                        }
                    }
                }
            }

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand("SELECT checksum, type FROM binaries", conn);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Server.Instance.GetCompileManager().AddType(reader.GetUInt32("checksum"), reader.GetString("type"));
                    }
                }
            }

            return nodeList;
        }

        public bool TryLogin(GameClient client, string tempUsername, string tempPass, out int homeId)
        {
            bool correctUser = false;
            homeId = -1;

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand("SELECT pass, homeComputer FROM accounts WHERE username = @0", conn);
                command.Parameters.Add(new MySqlParameter("0", tempUsername));

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.GetString("pass") == tempPass)
                        {
                            correctUser = true;
                            homeId = reader.GetInt32("homeComputer");
                            break;
                        }
                    }
                }
            }

            return correctUser;
        }

        public Dictionary<int, string> GetUsersInDatabase()
        {
            Dictionary<int, string> users = new Dictionary<int, string>();

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand("SELECT id, username FROM accounts", conn);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(reader.GetInt32("id"), reader.GetString("username"));
                    }
                }
            }

            return users;
        }

        public bool SetUserBanStatus(string user, int banExpiry, bool unban, bool permBan)
        {
            List<string> users = GetUsersInDatabase().Values.ToList();
            int userIndex = users.Count + 1; // The list is in inverted order for some reason idk of which is why we're subtracting from element count

            if (users.Contains(user) == false)
                return false;
            foreach (var user2 in users)
            {
                userIndex--;
                if (user2 == user)
                    break;
            }
            GameClient client = null;
            foreach (var client2 in Server.Instance.clients)
            {
                if (client2.username == user)
                {
                    client = client2;
                    break;
                }
            }
            try
            {
                client.Send(HackLinksCommon.NetUtil.PacketType.DSCON, "You have been banned from the server");
                client.netDisconnect();
            }
            catch (Exception) { }

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand($"UPDATE accounts SET banned = {banExpiry} WHERE id = {userIndex}", conn);
                if (unban)
                {
                    command.CommandText = $"UPDATE accounts SET banned = NULL, permBan = 0 WHERE id = {userIndex}";
                    command.ExecuteNonQuery();
                    return true;
                }
                if (permBan)
                {
                    command.CommandText = $"UPDATE accounts SET permBan = 1 WHERE id = {userIndex}";
                    command.ExecuteNonQuery();
                    return true;
                }
                command.ExecuteNonQuery();
                return true;
            }
        }

        public bool CheckUserBanStatus(string user, out int banExpiry)
        {
            Dictionary<string, int> bans = new Dictionary<string, int>();
            Dictionary<string, bool> permBans = new Dictionary<string, bool>();

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand("SELECT username, banned, permBanned FROM accounts", conn);
                using (MySqlDataReader reader =  command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(1))
                        {
                            if (reader.GetBoolean("permBanned"))
                                permBans.Add(reader.GetString("username"), true);
                            continue;
                        }
                        bans.Add(reader.GetString("username"), reader.GetInt32("banned"));
                        permBans.Add(reader.GetString("username"), reader.GetBoolean("permBanned"));
                    }
                }
            }

            try
            {
                if (permBans[user])
                {
                    banExpiry = 0;
                    return true;
                }
            }
            catch (Exception) { }

            try
            {
                if (bans[user] > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    banExpiry = bans[user];
                    return true;
                }
                if (bans[user] <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    SetUserBanStatus(user, 0, true, false);
            }
            catch (Exception) { }

            banExpiry = 0;
            return false;
        }

        public string GetUserNodes(string user)
        {
            List<string> nodes = new List<string>();
            string nodesString = "";

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand($"SELECT `netmap` FROM `accounts` WHERE `username` = '{user}'", conn);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        nodes.Add(reader.GetString("netmap"));
                    }
                }
            }

            foreach (var node in nodes)
            {
                if (nodesString == "")
                {
                    nodesString = node;
                    continue;
                }
                nodesString = nodesString + "," + node;
            }

            return nodesString;
        }

        public void AddUserNode(string user, string ip, string pos)
        {
            string nodes = GetUserNodes(user);

            if (nodes == "")
            {
                nodes = ip + ":" + pos;
            }
            else
            {
                nodes = nodes + "," + ip + ":" + pos;
            }

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand($"UPDATE accounts SET netmap = '{nodes}' WHERE '{user}' = `username`", conn);
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<string, List<Permissions>> GetUserPermissions()
        {
            Dictionary<string, List<Permissions>> permissionsDictionary = new Dictionary<string, List<Permissions>>();

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand("SELECT username, permissions FROM accounts", conn);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        List<Permissions> permissions = new List<Permissions>();
                        string[] permissionsString = reader.GetString("permissions").Split(',');
                        if (permissionsString.Contains("admin"))
                        {
                            permissions.Add(Permissions.Admin);
                        }
                        if (permissionsString.Contains("kick"))
                        {
                            permissions.Add(Permissions.Kick);
                        }
                        if (permissionsString.Contains("ban"))
                        {
                            permissions.Add(Permissions.Ban);
                        }
                        if (permissionsString.Contains("giveperms"))
                        {
                            permissions.Add(Permissions.Ban);
                        }
                        permissionsDictionary.Add(reader.GetString("username"), permissions);
                    }
                }
            }

            return permissionsDictionary;
        }

        public void SetUserPermissions(string user, List<Permissions> permissions)
        {
            string permissionsString = "";
            bool firstItem = true;
            if (permissions.Contains(Permissions.Admin))
            {
                if (firstItem)
                {
                    permissionsString = "admin";
                    firstItem = false;
                }
                else
                    permissionsString = permissionsString + ",admin";
            }
            if (permissions.Contains(Permissions.Kick))
            {
                if (firstItem)
                {
                    permissionsString = "kick";
                    firstItem = false;
                }
                else
                    permissionsString = permissionsString + ",kick";
            }
            if (permissions.Contains(Permissions.Ban))
            {
                if (firstItem)
                {
                    permissionsString = "ban";
                    firstItem = false;
                }
                else
                    permissionsString = permissionsString + ",ban";
            }
            if (permissions.Contains(Permissions.GivePerms))
            {
                if (firstItem)
                {
                    permissionsString = "giveperms";
                    firstItem = false;
                }
                else
                    permissionsString = permissionsString + ",giveperms";
            }

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand command = new MySqlCommand($"UPDATE accounts SET permissions = '{permissionsString}' WHERE '{user}' = `username`", conn);
                command.ExecuteNonQuery();
            }
        }

        public void LinkFile(int computerId, FileHandle fileHandle)
        {
            LinkFile(computerId, fileHandle.FilesystemId, fileHandle.Inode);
        }

        /// <summary>
        /// Increment the given file's link count. The actual directory modification should be done externally in the filesystem.
        /// </summary>
        /// <param name="computerId"></param>
        /// <param name=""></param>
        /// <param name="inodeID"></param>
        public void LinkFile(int computerId, ulong FilesystemId, ulong inodeID)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand incrementCommand = new MySqlCommand(
                "UPDATE files " +
                "SET link_count = link_count + 1 " +
                "WHERE computer_id = @computer_id AND filesystem_id = @filesystem_id AND inode = @inode", conn);
                incrementCommand.Parameters.AddRange(new MySqlParameter[] {
                    new MySqlParameter("computer_id", computerId),
                    new MySqlParameter("filesystem_id", FilesystemId),
                    new MySqlParameter("inode", inodeID),
                 });

                incrementCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Update the parent content.
        /// Decrement the file's link count and remove it from the db if it's zero.
        /// <b>IMPORTANT the parent files content must be manually altered to account for this</b>
        /// </summary>
        /// <param name="parentlisting"></param>
        /// <param name="parent"></param>
        /// <param name="fileHandle"></param>
        public void UnlinkFile(int computerId, FileHandle parent, FileHandle fileHandle)
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                MySqlCommand decrementCommand = new MySqlCommand(
                "UPDATE files " +
                "SET link_count = link_count - 1 " +
                "WHERE computer_id = @computer_id AND filesystem_id = @filesystem_id AND inode = @inode AND link_count > 0", conn);

                decrementCommand.Parameters.AddRange(new MySqlParameter[] {
                    new MySqlParameter("computer_id", computerId),
                    new MySqlParameter("filesystem_id", fileHandle.FilesystemId),
                    new MySqlParameter("inode", fileHandle.Inode),
                 });

                decrementCommand.ExecuteNonQuery();

                // This intentionally does nothing if the file still has links to it.
                MySqlCommand deleteCommand = new MySqlCommand(
                    "DELETE FROM files" +
                    " WHERE" +
                    " computer_id = @computer_id AND filesystem_id = @filesystem_id AND inode = @inode AND link_count <= 0"
                , conn);

                deleteCommand.Parameters.AddRange(new MySqlParameter[] {
                    new MySqlParameter("computer_id", computerId),
                    new MySqlParameter("filesystem_id", fileHandle.FilesystemId),
                    new MySqlParameter("inode", fileHandle.Inode),
                 });

                deleteCommand.ExecuteNonQuery();
            }

        }

        public static IEnumerable<T> Traverse<T>(IEnumerable<T> items,
        Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }

        public void RebuildDatabase()
        {
            Logger.Info("Rebuilding Database");

            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();

                foreach (string commandString in DatabaseDump.Commands)
                {
                    MySqlCommand command = new MySqlCommand(commandString, conn);
                    int res = command.ExecuteNonQuery();
                }
            }

            Logger.Info("Finished Rebuilding Database");
        }

        private bool UpdateDbFile(DiskInode child, MySqlConnection conn)
            {
                //    MySqlCommand fileCommand = new MySqlCommand(
                //        "INSERT INTO files" +
                //        " (computer_id, filesystem_id, inode, link_count, mode, group_id, user_id, inode_change_time, last_modified_time, last_accessed_time, content)" +
                //        " VALUES" +
                //        " (@id, @name, @parentFile, @type, @content, @computerId, @owner, @groupId, @permissions)" +
                //        " ON DUPLICATE KEY UPDATE" +
                //        " computer_id = @computer_id," +
                //        " filesystem_id = @filesystem_id," +
                //        " inode = @inode," +
                //        " link_count = @link_count," +
                //        " mode = @mode," +
                //        " group_id = @group_id," +
                //        " user_id = @user_id," +
                //        " inode_change_time = @inode_change_time," +
                //        " last_modified_time = @last_modified_time," +
                //        " last_accessed_time = @last_accessed_time," +
                //        " content = @content"
                //        , conn);
                //    fileCommand.Parameters.AddRange(new MySqlParameter[] {
                //                new MySqlParameter("computer_id", child.ID),
                //                new MySqlParameter("filesystem_id", child.ID),
                //                new MySqlParameter("inode", child.ID),
                //                new MySqlParameter("link_count", child.ID),
                //                new MySqlParameter("mode", child.ID),
                //                new MySqlParameter("group_id", child.ID),
                //                new MySqlParameter("user_id", child.ID),
                //                new MySqlParameter("inode_change_time", child.ID),
                //                new MySqlParameter("last_modified_time", child.ID),
                //                new MySqlParameter("last_accessed_time", child.ID),
                //                new MySqlParameter("content", child.ContentBytes),
                //            });

                //    int res = fileCommand.ExecuteNonQuery();

                //    int insertedId = (int)fileCommand.LastInsertedId;

                //return res > 0;
                return false;
        }
    }
}
