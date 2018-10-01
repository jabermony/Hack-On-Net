using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Util;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Permissions;

namespace HackLinks_Server.Computers.Processes
{
    public class Hackybox : CommandProcess
    {
        private static SortedDictionary<string, Tuple<string, Command>> commands = new SortedDictionary<string, Tuple<string, Command>>()
        {
            { "hackybox", new Tuple<string, Command>("hackybox COMMAND [args]\n    Executes the given HackyBox command with args.", CommandExec) },
            { "ping", new Tuple<string, Command>("ping [ip]\n    Outputs success if there is system online at the given IP.", Ping) },
            { "connect", new Tuple<string, Command>("connect [ip]\n    Connect to the system at the given IP.", Connect) },
            { "disconnect", new Tuple<string, Command>("disconnect \n    Terminate the current connection.", Disconnect) },
            { "dc", new Tuple<string, Command>("dc \n    Alias for disconnect.", Disconnect) },
            { "ls", new Tuple<string, Command>("ls \n    Lists all files in current directory.", Ls) },
            { "touch", new Tuple<string, Command>("touch [file]\n    Create the given file if it doesn't already exist.", Touch) },
            { "view", new Tuple<string, Command>("view [file]\n    Displays the given file on the Display Module.", View)},
            { "mkdir", new Tuple<string, Command>("mkdir [dir]\n    Create the given directory if it doesn't already exist.", MkDir) },
            { "rm", new Tuple<string, Command>("rm [file]\n    Remove the given file.", Remove) },
            { "login", new Tuple<string, Command>("login [username] [password]\n    Login to the current connected system using the given username and password.", Login) },
            { "chown", new Tuple<string, Command>("chown [file] [username]\n    Change the required user level for read and write operations on the given file.", ChOwn) },
            { "chmod", new Tuple<string, Command>("chmod [mode] [file]\n    Change the required user level for read and write operations on the given file.\n", ChMod) },
            { "fedit", new Tuple<string, Command>("fedit [append/line/remove/insert/help]\n     Edits the given file according to the mode used.", Fedit) },
            { "netmap", new Tuple<string, Command>("netmap [ip] [x] [y]\n    Adds a node to the network map", AddToNetMap) },
            { "music", new Tuple<string, Command>("music [(nameOfSong) OR shuffle OR list]", PlayMusic) },
        };

        public override SortedDictionary<string, Tuple<string, Command>> Commands => commands;

        public Hackybox(int pid, Printer printer, Node computer, Credentials credentials) : base(pid,  printer, computer, credentials)
        {
        }

        public static bool CommandExec(CommandProcess process, string[] command)
        {
            if(command.Length > 1)
            {
                return process.RunCommand(command[1]);
            }
            process.Print(commands[command[0]].Item1);
            return true;
        }

        public static bool Fedit(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : fedit [append/line/remove/insert/help]");
                return true;
            }
            var cmdArgs = command[1].Split(' ');
            if (cmdArgs[0] == "help")
            {
                process.Print("fedit [append] [file] [text] - Appends 'text' in a new line, at the bottom of the file.\n" +
                    "fedit [line] [file] [n] [text] - Changes content of line 'n' to 'text'.\n" +
                    "fedit [remove] [file] [n] - Removes line 'n' of the file.\n" +
                    "fedit [insert] [file] [n] [text] - Insert a new line containing 'text' in the 'n' line number.");
                return true;
            }
            if (cmdArgs[0] == "append")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Print("Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1]);
                if (file == null)
                {
                    process.Print("File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Print("Permission denied.");
                    return true;
                }
                // TODO use file streams instead ? 
                file.SetContent(file.GetContent() + '\n' + cmdArgs.JoinWords(" ", 2));
                process.Print("Content appended.");
                return true;
            }
            if (cmdArgs[0] == "line")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Print("Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1]);
                if (file == null)
                {
                    process.Print("File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Print("Permission denied.");
                    return true;
                }
                int n;
                if (!int.TryParse(cmdArgs[2], out n))
                {
                    process.Print("Wrong line number.");
                    return true;
                }
                int nth = file.GetContent().GetNthOccurence(n, '\n');
                string content = file.GetContent().Remove(nth, file.GetContent().GetNthOccurence(n + 1, '\n') - nth);
                file.SetContent(content.Insert(nth, '\n' + cmdArgs.JoinWords(" ", 3)));
                process.Print("Line edited.");
                return true;
            }
            if (cmdArgs[0] == "remove")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Print("Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1]);
                if (file == null)
                {
                    process.Print("File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Print("Permission denied.");
                    return true;
                }
                int n;
                if (!int.TryParse(cmdArgs[2], out n))
                {
                    process.Print("Wrong line number.");
                    return true;
                }
                var nth = file.GetContent().GetNthOccurence(n, '\n');
                file.SetContent(file.GetContent().Remove(nth, file.GetContent().GetNthOccurence(n + 1, '\n') - nth));
                process.Print("Line removed");
                return true;
            }
            if (cmdArgs[0] == "insert")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Print("Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1]);
                if (file == null)
                {
                    process.Print("File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Print("Permission denied.");
                    return true;
                }
                int n;
                if (!int.TryParse(cmdArgs[2], out n))
                {
                    process.Print("Wrong line number.");
                    return true;
                }
                file.SetContent(file.GetContent().Insert(file.GetContent().GetNthOccurence(n, '\n'), '\n' + cmdArgs.JoinWords(" ", 3)));
                process.Print("Content inserted");
                return true;
            }
            process.Print("Usage : fedit [append/line/remove/insert/help]");
            return true;
        }

        public static bool View(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : view [file]");
                return true;
            }
            var cmdArgs = command[1].Split(' ');
            if (cmdArgs.Length != 1)
            {
                process.Print("Usage : view [file]");
                return true;
            }
            var activeDirectory = process.ActiveDirectory;
            var file = activeDirectory.GetFile(cmdArgs[0]);
            if (file == null)
            {
                process.Print("File " + cmdArgs[0] + " not found.");
                return true;
            }
            if (file.Type.Equals(FileType.Directory))
            {
                process.Print("You cannot display a directory.");
                return true;
            }
            if (!file.HasReadPermission(process.Credentials))
            {
                process.Print("Permission denied.");
                return true;
            }

            process.Kernel.Display(process, "view", file.Name, file.GetContent());
            return true;
        }

        public static bool ChOwn(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print(commands[command[0]].Item1);
                return true;
            }

            var cmdArgs = command[1].Split(' ');

            if (cmdArgs.Length < 2)
            {
                process.Print(commands[command[0]].Item1);
                return true;
            }
            int pos = cmdArgs[1].IndexOf(':');

            string username;
            Permissions.Group? group;

            if (pos != -1)
            {
                username = cmdArgs[1].Substring(0, pos);
                string groupString = cmdArgs[1].Substring(pos + 1);
                group = Permissions.PermissionHelper.GetGroupFromString(groupString);
                if (group == Permissions.Group.INVALID)
                {
                    process.Print($"Invalid group '{groupString}' specified");
                    return true;
                }
            }
            else
            {
                username = cmdArgs[1];
                group = null;
            }

            if (!process.Kernel.HasUser(username))
            {
                process.Print($"User {username} does not exist!");
                return true;
            }

            var activeDirectory = process.ActiveDirectory;
            foreach (var file in activeDirectory.GetChildren())
            {
                if (file.Name == cmdArgs[0])
                {
                    if (file.OwnerId != process.Credentials.UserId)
                    {
                        process.Print("Permission denied. Only the current file owner may change file permissions.");
                        return true;
                    }
                    file.SetOwnerId(process.Kernel.GetUserId(username));
                    string message;
                    if (group.HasValue)
                    {
                        message = $"File {file.Name} owner changed to {username} and group set to {group}";
                    }
                    else
                    {
                        message = $"File {file.Name} owner changed to {username}";
                    }
                    process.Print(message);
                    return true;
                }
            }

            return true;
        }

        public static bool ChMod(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print(commands[command[0]].Item1);
                return true;
            }
            var cmdArgs = command[1].Split(' ');
            if (cmdArgs.Length != 2)
            {
                process.Print(commands[command[0]].Item1);
                return true;
            }
            var activePrivs = process.Credentials.Groups;

            var activeDirectory = process.ActiveDirectory;
            foreach (var fileC in activeDirectory.GetChildren())
            {
                if (fileC.Name == cmdArgs[1])
                {
                    if (process.Credentials.UserId != fileC.OwnerId)
                    {
                        process.Print("Permission denied.");
                        return true;
                    }

                    if (!Permissions.PermissionHelper.ApplyModifiers(cmdArgs[0], fileC.PermissionValue, out int outValue))
                    {
                        process.Print($"Invalid mode '{cmdArgs[0]}'\r\nUsage : chmod [permissions] [file]");
                        return true;
                    }

                    process.Kernel.SetFilePermissionValue(process, fileC.FileHandle, outValue);

                    process.Print($"File {fileC.Name} permissions changed. to {fileC.PermissionValue}");

                    return true;
                }
            }
            process.Print("File " + cmdArgs[1] + " was not found.");
            return true;
        }

        public static bool Login(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : login [username] [password]");
                return true;
            }
            var args = command[1].Split(' ');
            if (args.Length < 2)
            {
                process.Print("Usage : login [username] [password]");
                return true;
            }
            process.Kernel.Login(process, args[0], args[1]);
            return true;
        }

        public static bool Ping(CommandProcess process, string[] command)
        {
            //TODO ping
            //var compManager = client.server.GetComputerManager();
            //if (command.Length < 2)
            //{
            //    process.Print("Usage : ping [ip]");
            //    return true;
            //}
            //var connectingToNode = compManager.GetNodeByIp(command[1]);
            //if (connectingToNode == null)
            //{
            //    process.Print("Ping on " + command[1] + " timeout.");
            //    return true;
            //}
            //process.Print("Ping on " + command[1] + " success.");
            return true;
        }

        public static bool Connect(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : command [ip]");
                return true;
            }

            process.Kernel.Connect(process, command[1]);

            return true;
        }

        public static bool Disconnect(CommandProcess process, string[] command)
        {
            process.Kernel.Disconnect(process);

            return true;
        }

        // TOOD use file timestamps in LS output somehow.
        public static bool Ls(CommandProcess process, string[] command)
        {
            if (command.Length == 2)
            {
                foreach (File file in process.ActiveDirectory.GetChildren())
                {
                    if (command[1] == file.Name)
                    {
                        process.Print($"{file.Type} {file.Name} > Owner '{process.Kernel.GetUsername(file.OwnerId)}' Group '{file.Group}' Permissions '{PermissionHelper.PermissionToDisplayString(file.PermissionValue)}'");
                        return true;
                    }
                }
                process.Print("File " + command[1] + " not found.");
                return true;
            }
            else
            {
                List<string> fileList = new List<string>(new string[] { process.ActiveDirectory.Name });
                foreach (File file in process.ActiveDirectory.GetChildren())
                {
                    if (file.HasReadPermission(process.Credentials))
                    {
                        fileList.AddRange(new string[] {
                                file.Name, (file.Type.Equals(FileType.Directory) ? "d" : "f"), (file.HasWritePermission(process.Credentials) ? "w" : "-")
                            });
                    }
                    else
                    {
                        Logger.Warning($"User {process.Kernel.GetUsername(process.Credentials.UserId)} doesn't have permission for {file.Name} {file.Group} {PermissionHelper.PermissionToDisplayString(file.PermissionValue)}");
                    }
                }
                process.Kernel.LS(process, fileList.ToArray());
                return true;
            }
        }

        //TODO update file timestamps on touch. Like linux.
        public static bool Touch(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : touch [fileName]");
            }

            var activeDirectory = process.ActiveDirectory;
            foreach (var fileC in activeDirectory.GetChildren())
            {
                if (fileC.Name == command[1])
                {
                    process.Print("File " + command[1] + " touched.");
                    return true;
                }
            }
            if (!activeDirectory.HasWritePermission(process.Credentials))
            {
                process.Print("Permission denied.");
                return true;
            }

            File file = process.Kernel.CreateFile(process, activeDirectory, command[1], Permission.A_All, process.Credentials.UserId, process.Credentials.Group);

            process.Print("File " + command[1]);
            return true;
        }

        public static bool Remove(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : rm [fileName]");
            }
            var activeDirectory = process.ActiveDirectory;
            foreach (var file in activeDirectory.GetChildren())
            {
                if (file.Name == command[1])
                {
                    if (!file.HasWritePermission(process.Credentials))
                    {
                        process.Print("Permission denied.");
                        return true;
                    }
                    process.Print("File " + command[1] + " removed.");

                    process.Kernel.UnlinkFile(process, file.FileHandle);
                    return true;
                }
            }



            process.Print("File does not exist.");
            return true;
        }

        public static bool MkDir(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : mkdir [folderName]");
                return true;
            }

            var activeDirectory = process.ActiveDirectory;
            foreach (var fileC in activeDirectory.GetChildren())
            {
                if (fileC.Name == command[1])
                {
                    process.Print("Folder " + command[1] + " already exists.");
                    return true;
                }
            }

            bool passed = activeDirectory.HasWritePermission(process.Credentials);

            if (!passed)
            {
                process.Print("Permission denied.");
                return true;
            }

            File file = process.Kernel.CreateFile(process, activeDirectory, command[1], Permission.A_All & ~Permission.O_All, process.Credentials.UserId, process.Credentials.Group, FileType.Directory);

            return true;
        }

        public static bool AddToNetMap(CommandProcess process, string[] commandUnsplit)
        {
            List<string> command = new List<string>();
            command.Add("netmap");
            command.AddRange(commandUnsplit[1].Split());
            if (command.Count < 4)
            {
                process.Print("Usage: netmap [ip] [x] [y]");
                return true;
            }
            //TODO kernel
            //Server.Instance.DatabaseLink.AddUserNode(client.username, command[1], command[2] + ":" + command[3]);
            return true;
        }

        public static bool PlayMusic(CommandProcess process, string[] commandUnsplit) {
            try {
                List<string> command = new List<string>();
                command.Add("music");
                command.AddRange(commandUnsplit[1].Split());
                if (command.Count < 2) {
                    process.Print("Usage: music [(nameofsong) (Note: Must be in a folder called \"HNMPMusic\" in the Mods folder as an .wav file.)]\nOR music shuffle\nOR music list");
                    return true;
                }
                process.Kernel.PlayMusic(process, command[1]);
                return true;
            } catch(ObjectDisposedException e) {
                Logger.Exception(e);
                return true;
            } catch(Exception e) {
                Logger.Exception(e);
                return true;
            }
        }
    }
}
