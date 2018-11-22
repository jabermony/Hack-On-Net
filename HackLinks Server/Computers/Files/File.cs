using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HackLinks_Server.Files
{
    public class File
    {
        private Kernel Kernel { get; }
        public readonly FileDescriptor FileDescriptor;

        public string Name => FileDescriptor.Name;

        public Permission PermissionValue => Kernel.GetPermissions(FileDescriptor);

        public int OwnerId => Kernel.GetFileOwnerId(FileDescriptor);

        public Group Group => Kernel.GetFileGroup(FileDescriptor);

        public File Parent => Kernel.GetParentFile(this, ref lastError);

        public FileType Type => Kernel.GetFileType(FileDescriptor);

        public Filesystem.Error lastError = Filesystem.Error.None;
        public Filesystem.Error LastError => LastError;

        public File(FileDescriptor fileDescriptor, Kernel kernel)
        {
            FileDescriptor = fileDescriptor;
            Kernel = kernel;
        }

        public File GetFile(string name, FileDescriptor.Flags flags)
        {
            return Kernel.GetFileAt(this, name, flags, ref lastError);
        }

        public byte[] GetContent()
        {
            lastError = Filesystem.Error.None;
            return Kernel.GetFileContent(FileDescriptor, ref lastError);
        }

        public string GetContentString()
        {
            byte[] content = GetContent();
            if (lastError != Filesystem.Error.None)
            {
                return "";
            }
            else
            {
                return Encoding.UTF8.GetString(content);
            }
        }

        public void SetContent(string value)
        {
            lastError = Filesystem.Error.None;
            Kernel.SetContent(FileDescriptor, value, ref lastError);
        }

        public List<FileUtil.DirRecord> GetChildren()
        {
            return Kernel.GetDirectoryList(FileDescriptor);
        }

        public void SetPermissionValue(Permission value)
        {
            lastError = Filesystem.Error.None;
            Kernel.SetPermission(FileDescriptor, value, ref lastError);
        }

        public void SetGroup(Group value)
        {
            lastError = Filesystem.Error.None;
            Kernel.SetGroup(FileDescriptor, value, ref lastError);
        }

        public void SetOwnerId(int value)
        {
            lastError = Filesystem.Error.None;
            Kernel.SetOwnerId(FileDescriptor, value, ref lastError);
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

        public bool HasPermission(Permission value, Credentials credentials)
        {
            return HasPermission(value, credentials.UserId, credentials.Group);
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

        public bool HasPermission(Permission value, int userId, params Group[] privs)
        {
            return Kernel.CheckPermission(FileDescriptor, value, userId, privs);
        }
    }
}