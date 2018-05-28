using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;

namespace HackLinks_Server.Files
{
    public abstract class File
    {
        /// <summary>FilType determines how a file will be handled by the system</summary>
        public enum FileType
        {
            Regular,
            Directory,
            Link,
            LOG,
            Special, // E.G. Character devices, Block devices, and Sockets
        }

        public readonly int id;

        private string name;
        private int ownerId;
        private Group group;

        private File parent;
        private int parentId;
        public int computerId;

        public bool Dirty { get; set; }

        public string Name { get => name; set { name = value; Dirty = true; } }

        public FilePermissions Permissions { get; set; }

        public int OwnerId { get => ownerId; set { ownerId = value; Dirty = true; } }

        public Group Group { get => group; set { group = value; Dirty = true; } }

        internal File Parent
        {
            get => parent;
            set
            {
                if (parent != null)
                {
                    parent.children.RemoveAll(child => child.id == id);
                }

                parent = value;
                if (parent != null)
                {
                    ParentId = parent.id;
                }
            }
        }

        public int ParentId { get => parentId; set { parentId = value; Dirty = true; } }
        public int ComputerId { get => computerId; set { computerId = value; Dirty = true; } }

        public List<File> children = new List<File>();

        private FileType type = FileType.Regular;
        public FileType Type { get => type; set { type = value; Dirty = true; } }

        public abstract string Content { get; set; }
        public abstract int Checksum { get; }

        protected File(int id, Node computer, File parent, string name)
        {
            this.id = id;
            this.computerId = computer.id;
            this.Name = name;
            this.Parent = parent;
            if (parent != null)
            {
                this.Parent.children.Add(this);
            }
            Permissions = new FilePermissions(this);
        }

        public File GetFile(string name)
        {
            foreach (File file in children)
            {
                if (file.Name == name)
                    return file;
            }
            return null;
        }

        public bool HasExecutePermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, false, false, true);
        }

        public bool HasWritePermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, false, true, false);
        }

        public bool HasReadPermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, true, false, false);
        }

        public bool HasExecutePermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, false, false, true);
        }

        public bool HasWritePermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, false, true, false);
        }

        public bool HasReadPermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, true, false, false);
        }

        public bool HasExecutePermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, false, false, true);
        }

        public bool HasWritePermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, false, true, false);
        }

        public bool HasReadPermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, true, false, false);
        }

        public bool HasPermission(int userId, Group priv, bool read, bool write, bool execute)
        {
            return HasPermission(userId, new List<Group> { priv }, read, write, execute);
        }

        public bool HasPermission(int userId, List<Group> privs, bool read, bool write, bool execute)
        {
            if (privs.Contains(Group))
            {
                if (Permissions.CheckPermission(FilePermissions.PermissionType.Group, read, write, execute))
                {
                    return true;
                }
            }

            if (OwnerId == userId)
            {
                if (Permissions.CheckPermission(FilePermissions.PermissionType.User, read, write, execute))
                {
                    return true;
                }
            }

            return Permissions.CheckPermission(FilePermissions.PermissionType.Others, read, write, execute);
        }

        public File GetFileAtPath(string path)
        {
            string[] pathSteps = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            File activeFolder = this;
            for (int i = 0; i < pathSteps.Length - 1; i++)
            {
                var folder = activeFolder.GetFile(pathSteps[i]);
                if (folder == null || !folder.Type.Equals(FileType.Directory))
                    return null;
                activeFolder = folder;
            }
            return activeFolder.GetFile(pathSteps[pathSteps.Length - 1]);
        }

        public void PrintFolderRecursive(int depth)
        {
            string tabs = new String(' ', depth);
            Logger.Debug(tabs + id + "  d- " + Name);
            foreach (var item in children)
            {
                if (item.Type.Equals(FileType.Directory))
                {
                    item.PrintFolderRecursive(depth + 1);
                }
                else
                {
                    Logger.Debug(tabs + " " + item.id + "  f- " + item.Name);
                }
            }
        }

        public void SetType(int specType)
        {
            Type = (FileType)specType;
        }

        virtual public void RemoveFile()
        {
            Parent.children.Remove(this);
            ParentId = 0;
            if (Type == FileType.LOG)
            {
                Log log = null;
                foreach (var log2 in Server.Instance.GetComputerManager().GetNodeById(ComputerId).logs)
                {
                    if (log2.file == this)
                    {
                        log = log2;
                        break;
                    }
                }
                Server.Instance.GetComputerManager().GetNodeById(ComputerId).logs.Remove(log);
            }
        }
    }
}