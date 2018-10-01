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

namespace HackLinks_Server.Computers
{
    public class ComputerManager
    {
        Server server;

        private List<Node> nodeList = new List<Node>();
        private List<File> toDelete = new List<File>();

        public List<Node> NodeList => nodeList;

        public ComputerManager(Server server, List<Node> nodeList)
        {
            this.server = server;
            this.nodeList = nodeList;
        }

        public void Init()
        {
            // Init all the nodes!
            foreach(Node node in NodeList)
            {
                node.Init();
            }
        }

        public Node GetNodeByIp(string ip)
        {
            foreach(Node node in nodeList)
            {
                if (node.ip == ip)
                    return node;
            }
            return null;
        }

        public Node GetNodeById(int homeId)
        {
            foreach (Node node in nodeList)
                if (node.id == homeId)
                    return node;
            return null;
        }

        public void AddToDelete(File file)
        {
            toDelete.Add(file);
        }
    }
}
