using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HackLinks_Server.Files
{
    public class File
    {
        private Kernel Kernel => process.Kernel;
        public readonly FileHandle FileHandle;
        private readonly Process process;

        public string Name => FileHandle.Name;

        public int PermissionValue => Kernel.GetFilePermissionValue(FileHandle);

        public int OwnerId => Kernel.GetFileOwnerId(FileHandle);

        public Group Group => Kernel.GetFileGroup(FileHandle);

        public File Parent => Kernel.GetParentFile(process, this);

        public FileType Type => Kernel.GetFileType(FileHandle);

        public uint Checksum => Kernel.GetFileChecksum(FileHandle);

        public File(Process parentProcess, FileHandle fileHandle)
        {
            FileHandle = fileHandle;
            process = parentProcess;
        }

        public File GetFile(string name)
        {
            return Kernel.GetFileAt(process, this, name);
        }

        public string GetContent()
        {
            return Kernel.GetFileContent(process, FileHandle);
        }

        public void SetContent(string value)
        {
            Kernel.SetFileContent(process, FileHandle, value);
        }

        public List<File> GetChildren()
        {
            return Kernel.GetChildren(process, this);
        }

        public bool SetPermissionValue(int value)
        {
            return Kernel.SetFilePermissionValue(process, FileHandle, value);
        }

        public bool SetGroup(Group value)
        {
            return Kernel.SetFileGroup(process, FileHandle, value);
        }

        public bool SetOwnerId(int value)
        {
            return Kernel.SetFileOwnerId(process, FileHandle, value);
        }

        public bool HasExecutePermission(Credentials credentials)
        {
            return HasPermission(PermissionClass.Execute, credentials.UserId, credentials.Group);
        }

        public bool HasWritePermission(Credentials credentials)
        {
            return HasPermission(PermissionClass.Write, credentials.UserId, credentials.Group);
        }

        public bool HasReadPermission(Credentials credentials)
        {
            return HasPermission(PermissionClass.Read, credentials.UserId, credentials.Group);
        }

        public bool HasPermission(PermissionClass value, Credentials credentials)
        {
            return HasPermission(value, credentials.UserId, credentials.Group);
        }

        public bool HasExecutePermission(int userId, params Group[] privs)
        {
            return HasPermission(PermissionClass.Execute, userId, privs);
        }

        public bool HasWritePermission(int userId, params Group[] privs)
        {
            return HasPermission(PermissionClass.Write, userId, privs);
        }

        public bool HasReadPermission(int userId, params Group[] privs)
        {
            return HasPermission(PermissionClass.Read, userId, privs);
        }

        public bool HasPermission(PermissionClass value, int userId, params Group[] privs)
        {
            return Kernel.CheckPermission(FileHandle, value, userId, privs);
        }
    }
}