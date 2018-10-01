using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Filesystems;

namespace HackLinks_Server.Util
{
    class DBUtil
    {
        internal static int GenerateMode(FileType type, Permission permission)
        {
            return (int)type << 9 | (int)permission;
        }
    }
}
