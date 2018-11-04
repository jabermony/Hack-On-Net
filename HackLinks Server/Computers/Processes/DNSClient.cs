using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Daemons;
using HackLinks_Server.Computers.Processes.Daemons;
using HackLinks_Server.Computers.Processes.Daemons.Dns;
using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    class DNSClient : DaemonClient
    {

        public SortedDictionary<string, Tuple<string, Command>> daemonCommands = new SortedDictionary<string, Tuple<string, Command>>()
        {
            { "dns", new Tuple<string, Command>("dns [lookup/rlookup] [URL/IP]\n    Get the lookup of the specified URL/IP.", Dns) },
        };

        public override SortedDictionary<string, Tuple<string, Command>> Commands
        {
            get => daemonCommands;
        }

        public DNSClient(Session session, Daemon daemon, int pid, Node computer, Credentials credentials) : base(session, daemon, pid, computer, credentials)
        {

        }

        public static bool Dns(CommandProcess process, string[] command)
        {
            DNSClient client = (DNSClient) process;
            DNSDaemon daemon = (DNSDaemon) client.Daemon;

            if (command[0] == "dns")
            {
                if (command.Length < 2)
                {
                    process.Kernel.Print(process, "Usage : dns [lookup/rlookup] [URL/IP]");
                    return true;
                }
                var cmdArgs = command[1].Split(' ');
                if (cmdArgs[0] == "update")
                {
                    if (PermissionHelper.CheckCredentials(process.Credentials, Group.ADMIN))
                    {
                        process.Kernel.Print(process, "Permission denied");
                        return true;
                    }
                    daemon.LoadEntries();
                    process.Kernel.Print(process, "Successfully updated the DNS.");
                    return true;
                }
                if (cmdArgs[0] == "lookup")
                {
                    var url = cmdArgs[1];
                    var ip = daemon.LookUp(url);
                    process.Kernel.Print(process, "Result IP : " + (ip ?? "unknown"));
                    return true;
                }
                if (cmdArgs[0] == "rlookup")
                {
                    var ip = cmdArgs[1];
                    var url = daemon.RLookUp(ip);
                    process.Kernel.Print(process, "Result URL : " + (url ?? "unknown"));
                    return true;
                }
                if (cmdArgs[0] == "assign")
                {
                    if (PermissionHelper.CheckCredentials(process.Credentials, Group.ADMIN))
                    {
                        process.Kernel.Print(process, "Insufficient permission.");
                        return true;
                    }
                    if (cmdArgs.Length <= 2)
                    {
                        process.Kernel.Print(process, "Missing arguments.\nProper usage: dns assign [IP] [URL]");
                        return true;
                    }
                    File dnsFolder = process.Kernel.GetFile(process, "/dns");
                    if (dnsFolder == null)
                    {
                        dnsFolder = process.Kernel.CreateFile(process, process.Kernel.GetFile(process, "/"), "dns", Permission.A_All & ~Permission.O_All, 0, process.Credentials.Group, FileType.Directory);
                    }
                    else
                    {
                        if (!dnsFolder.Type.Equals(FileType.Directory))
                            return true;
                    }
                    File dnsEntries = dnsFolder.GetFile("entries.db");
                    if (dnsEntries == null)
                    {
                        dnsEntries = process.Kernel.CreateFile(process, dnsFolder, "entries.db", Permission.A_All & ~Permission.O_All, 0, Group.ADMIN);
                    }
                    else if (dnsEntries.Type.Equals(FileType.Directory))
                    {
                        process.Kernel.UnlinkFile(process, dnsEntries.FileHandle);
                        dnsEntries = process.Kernel.CreateFile(process, dnsFolder, "entries.db", Permission.A_All & ~Permission.O_All, 0, Group.ADMIN);
                    }
                    foreach (DNSEntry entry in daemon.entries)
                    {
                        if (entry.Url == cmdArgs[2])
                        {
                            process.Kernel.Print(process, "The provided URL is already assigned an IP address.");
                            return true;
                        }
                    }
                    dnsEntries.SetContent(dnsEntries.GetContent() + '\n' + cmdArgs[1] + '=' + cmdArgs[2]);
                    daemon.LoadEntries();
                    process.Kernel.Print(process, "Content appended.");
                    return true;
                }
                process.Kernel.Print(process, "Usage : dns [lookup/rlookup] [URL/IP]");
                return true;
            }
            return false;
        }
    }
}
