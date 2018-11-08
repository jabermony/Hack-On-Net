using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace HackLinks_Server.Computers.Filesystems
{
    public abstract class Inode
    {
        protected Filesystem Filesystem { get; set; }
        public ulong FileSystemId => Filesystem.ID;

        public ulong ID { get; private set; }
        public int ComputerID => Filesystem.ComputerID;

        public bool Dirty { get; set; }

        // File Mode is composed of these values: Permissions, OwnerId, Group, Type.
        private int mode;
        public int PermissionValue { get => mode & 0b000111111111; set => mode = (mode & 0b111000000000) | value; }
        public int OwnerId { get; set; }
        public Group Group { get; set; }
        public FileType Type { get => (FileType)(mode >> 9 & 0b111); protected set => mode = (mode & 0b000111111111) | (int)value << 9; }

        public abstract Stream ContentStream { get; }
        public abstract uint Checksum { get; }

        protected Inode(Filesystem fileSystem, ulong id, int mode)
        {
            ID = id;
            Filesystem = fileSystem;
            this.mode = mode;
        }

        public bool HasExecutePermission(Credentials credentials)
        {
            return HasPermission(Permission.A_Execute, credentials.UserId, credentials.Group);
        }

        public bool HasWritePermission(Credentials credentials)
        {
            return HasPermission(Permission.A_Write, credentials.UserId, credentials.Group);
        }

        public bool HasReadPermission(Credentials credentials)
        {
            return HasPermission(Permission.A_Read, credentials.UserId, credentials.Group);
        }

        public bool HasExecutePermission(int userId, params Group[] privs)
        {
            return HasPermission(Permission.A_Execute, userId, privs);
        }

        public bool HasWritePermission(int userId, params Group[] privs)
        {
            return HasPermission(Permission.A_Write, userId, privs);
        }

        public bool HasReadPermission(int userId, params Group[] privs)
        {
            return HasPermission(Permission.A_Read, userId, privs);
        }

        public bool HasPermission(Permission mask, int userId, params Group[] privs)
        {
            return PermissionHelper.CheckPermission(mask, PermissionValue, OwnerId, Group, userId, privs);
        }
    }
}