using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using System;

namespace HackLinks_Server.Computers.Filesystems
{
    public enum Permission
    {
        None = 0b000_000_000,

        O_All     = 0b000_000_111,
        O_Execute = 0b000_000_001,
        O_Write   = 0b000_000_010,
        O_Read    = 0b000_000_100,

        G_All     = 0b000_111_000,
        G_Execute = 0b000_001_000,
        G_Write   = 0b000_010_000,
        G_Read    = 0b000_100_000,

        U_All     = 0b111_000_000,
        U_Execute = 0b001_000_000,
        U_Write   = 0b010_000_000,
        U_Read    = 0b100_000_000,

        A_All     = 0b111_111_111,
        A_Execute = 0b001_001_001,
        A_Write   = 0b010_010_010,
        A_Read    = 0b100_100_100,
    }

    public enum PermissionClass
    {
        Execute = 0b001_001_001,
        Write =   0b010_010_010,
        Read =    0b100_100_100,
    }
}
