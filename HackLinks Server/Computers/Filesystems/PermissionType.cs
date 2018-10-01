using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using System;

namespace HackLinks_Server.Computers.Filesystems
{
    public enum PermissionType
    {
        User = 0b111_000_000,
        Group = 0b000_111_000,
        Others = 0b000_000_111,
        All = 0b111_111_111,
    }
}
