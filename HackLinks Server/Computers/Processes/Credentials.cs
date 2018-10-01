using HackLinks_Server.Computers.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    public class Credentials
    {
        public int UserId { get; private set; }
        public Group Group { get; private set; }
        public Group[] Groups { get; private set; }

        public Credentials(int userId, Group group)
        {
            UserId = userId;
            Group = group;
            Groups = new Group[0];
        }

        public Credentials(int userId, Group group, Group[] groups)
        {
            UserId = userId;
            Group = group;
            Groups = groups.ToArray();
        }

        public Credentials(int userId, Group group, List<Group> groups) 
            : this(userId, group, groups.ToArray())
        {
           
        }
    }
}
