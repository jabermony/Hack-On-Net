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
            { "ln", new Tuple<string, Command>("ln [source] [target]\n    Hard links the given file.", Link) },
        };

        public override SortedDictionary<string, Tuple<string, Command>> Commands => commands;

        public Hackybox(int pid, Node computer, Credentials credentials) : base(pid, computer, credentials)
        {
        }

        public static bool CommandExec(CommandProcess process, string[] command)
        {
            if(command.Length > 1)
            {
                return process.RunCommand(command[1]);
            }
            process.Kernel.Print(process, commands[command[0]].Item1);
            return true;
        }

        public static bool Fedit(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;
            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : fedit [append/line/remove/insert/help]");
                return true;
            }
            var cmdArgs = command[1].Split(' ');
            if (cmdArgs[0] == "help")
            {
                process.Kernel.Print(process, "fedit [append] [file] [text] - Appends 'text' in a new line, at the bottom of the file.\n" +
                    "fedit [line] [file] [n] [text] - Changes content of line 'n' to 'text'.\n" +
                    "fedit [remove] [file] [n] - Removes line 'n' of the file.\n" +
                    "fedit [insert] [file] [n] [text] - Insert a new line containing 'text' in the 'n' line number.");
                return true;
            }
            if (cmdArgs[0] == "append")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Kernel.Print(process, "Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1], FileDescriptor.Flags.Read_Write);
                if (file == null)
                {
                    process.Kernel.Print(process, "File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Kernel.Print(process, "Permission denied.");
                    return true;
                }
                // TODO use file streams instead ? 
                file.SetContent(file.GetContentString() + '\n' + cmdArgs.JoinWords(" ", 2));
                process.Kernel.Print(process, "Content appended.");
                return true;
            }
            if (cmdArgs[0] == "line")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Kernel.Print(process, "Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1], FileDescriptor.Flags.Read_Write);
                if (file == null)
                {
                    process.Kernel.Print(process, "File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Kernel.Print(process, "Permission denied.");
                    return true;
                }
                int n;
                if (!int.TryParse(cmdArgs[2], out n))
                {
                    process.Kernel.Print(process, "Wrong line number.");
                    return true;
                }
                int nth = file.GetContentString().GetNthOccurence(n, '\n');
                string content = file.GetContentString().Remove(nth, file.GetContentString().GetNthOccurence(n + 1, '\n') - nth);
                file.SetContent(content.Insert(nth, '\n' + cmdArgs.JoinWords(" ", 3)));
                process.Kernel.Print(process, "Line edited.");
                return true;
            }
            if (cmdArgs[0] == "remove")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Kernel.Print(process, "Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1], FileDescriptor.Flags.Read_Write);
                if (file == null)
                {
                    process.Kernel.Print(process, "File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Kernel.Print(process, "Permission denied.");
                    return true;
                }
                int n;
                if (!int.TryParse(cmdArgs[2], out n))
                {
                    process.Kernel.Print(process, "Wrong line number.");
                    return true;
                }
                var nth = file.GetContentString().GetNthOccurence(n, '\n');
                file.SetContent(file.GetContentString().Remove(nth, file.GetContentString().GetNthOccurence(n + 1, '\n') - nth));
                process.Kernel.Print(process, "Line removed");
                return true;
            }
            if (cmdArgs[0] == "insert")
            {
                if (cmdArgs.Length < 3)
                {
                    process.Kernel.Print(process, "Missing arguments");
                    return true;
                }
                var file = process.ActiveDirectory.GetFile(cmdArgs[1], FileDescriptor.Flags.Read_Write);
                if (file == null)
                {
                    process.Kernel.Print(process, "File " + cmdArgs[1] + " not found.");
                    return true;
                }
                if (!file.HasWritePermission(process.Credentials))
                {
                    process.Kernel.Print(process, "Permission denied.");
                    return true;
                }
                int n;
                if (!int.TryParse(cmdArgs[2], out n))
                {
                    process.Kernel.Print(process, "Wrong line number.");
                    return true;
                }
                file.SetContent(file.GetContentString().Insert(file.GetContentString().GetNthOccurence(n, '\n'), '\n' + cmdArgs.JoinWords(" ", 3)));
                process.Kernel.Print(process, "Content inserted");
                return true;
            }
            process.Kernel.Print(process, "Usage : fedit [append/line/remove/insert/help]");
            return true;
        }

        public static bool View(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : view [file]");
                return true;
            }
            var cmdArgs = command[1].Split(' ');
            if (cmdArgs.Length != 1)
            {
                process.Kernel.Print(process, "Usage : view [file]");
                return true;
            }
            var activeDirectory = process.ActiveDirectory;
            var file = activeDirectory.GetFile(cmdArgs[0], FileDescriptor.Flags.Read);
            if (file == null)
            {
                process.Kernel.Print(process, "File " + cmdArgs[0] + " not found.");
                return true;
            }
            if (file.Type.Equals(FileType.Directory))
            {
                process.Kernel.Print(process, "You cannot display a directory.");
                return true;
            }
            if (!file.HasReadPermission(process.Credentials))
            {
                process.Kernel.Print(process, "Permission denied.");
                return true;
            }

            process.Kernel.Display(process, "view", file.Name, file.GetContentString());
            return true;
        }

        public static bool ChOwn(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;

            if (command.Length < 2)
            {
                process.Kernel.Print(process, commands[command[0]].Item1);
                return true;
            }

            var cmdArgs = command[1].Split(' ');

            if (cmdArgs.Length < 2)
            {
                process.Kernel.Print(process, commands[command[0]].Item1);
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
                    process.Kernel.Print(process, $"Invalid group '{groupString}' specified");
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
                process.Kernel.Print(process, $"User {username} does not exist!");
                return true;
            }

            var activeDirectory = process.ActiveDirectory;

            File targ = process.Kernel.GetFileAt(activeDirectory, cmdArgs[0], FileDescriptor.Flags.Write, ref error);
            if ((error & Filesystem.Error.Permission_Denied) == Filesystem.Error.Permission_Denied)
            {
                process.Kernel.Print(process, "Permission denied.");
                return true;
            }

            if (targ != null)
            {
                process.Kernel.SetOwnerId(targ.FileDescriptor, process.Kernel.GetUserId(username), ref error);
                string message;
                if (group.HasValue)
                {
                    message = $"File {targ.Name} owner changed to {username} and group set to {group}";
                }
                else
                {
                    message = $"File {targ.Name} owner changed to {username}";
                }
                process.Kernel.Print(process, message);
                return true;
            }

            return true;
        }

        public static bool ChMod(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;

            if (command.Length < 2)
            {
                process.Kernel.Print(process, commands[command[0]].Item1);
                return true;
            }
            var cmdArgs = command[1].Split(' ');
            if (cmdArgs.Length != 2)
            {
                process.Kernel.Print(process, commands[command[0]].Item1);
                return true;
            }
            var activePrivs = process.Credentials.Groups;

            var activeDirectory = process.ActiveDirectory;

            FileDescriptor targ = process.Kernel.OpenAt(activeDirectory.FileDescriptor, cmdArgs[1], FileDescriptor.Flags.Write, Permission.None, ref error);
            if ((error & Filesystem.Error.Permission_Denied) == Filesystem.Error.Permission_Denied)
            {
                process.Kernel.Print(process, "Permission denied.");
                return true;
            }

            if (targ != null)
            {
                File fileC = new File(targ, process.Kernel);
                if (!Permissions.PermissionHelper.ApplyModifiers(cmdArgs[0], fileC.PermissionValue, out Permission outValue))
                {
                    process.Kernel.Print(process, $"Invalid mode '{cmdArgs[0]}'\r\nUsage : chmod [permissions] [file]");
                    return true;
                } else
                {
                    process.Kernel.SetPermission(targ, outValue, ref error);

                    process.Kernel.Print(process, $"File {fileC.Name} permissions changed. to {fileC.PermissionValue}");

                    return true;
                }
            }
            process.Kernel.Print(process, "File " + cmdArgs[1] + " was not found.");
            return true;
        }

        public static bool Login(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : login [username] [password]");
                return true;
            }
            var args = command[1].Split(' ');
            if (args.Length < 2)
            {
                process.Kernel.Print(process, "Usage : login [username] [password]");
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
            //    process.Kernel.Print(process, "Usage : ping [ip]");
            //    return true;
            //}
            //var connectingToNode = compManager.GetNodeByIp(command[1]);
            //if (connectingToNode == null)
            //{
            //    process.Kernel.Print(process, "Ping on " + command[1] + " timeout.");
            //    return true;
            //}
            //process.Kernel.Print(process, "Ping on " + command[1] + " success.");
            return true;
        }

        public static bool Connect(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : command [ip]");
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
            Filesystem.Error error = Filesystem.Error.None;
            if (command.Length == 2)
            {
                foreach (FileUtil.DirRecord rec in process.ActiveDirectory.GetChildren())
                {
                    File file = process.Kernel.GetFileAt(process.ActiveDirectory, rec.name, FileDescriptor.Flags.None, ref error);
                    if (error == Filesystem.Error.None && command[1] == file.Name)
                    {
                        process.Kernel.Print(process, $"{file.Type} {file.Name} > Owner '{process.Kernel.GetUsername(file.OwnerId)}' Group '{file.Group}' Permissions '{PermissionHelper.PermissionToDisplayString(file.PermissionValue)}'");
                        return true;
                    }
                }
                process.Kernel.Print(process, "File " + command[1] + " not found.");
                return true;
            }
            else
            {
                List<string> fileList = new List<string>(new string[] { process.ActiveDirectory.Name });
                foreach (FileUtil.DirRecord rec in process.ActiveDirectory.GetChildren())
                {
                    File file = process.Kernel.GetFileAt(process.ActiveDirectory, rec.name, FileDescriptor.Flags.None, ref error);
                    if (error == Filesystem.Error.None)
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
            Filesystem.Error error = Filesystem.Error.None;
            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : touch [fileName]");
            }

            var activeDirectory = process.ActiveDirectory;
            foreach (FileUtil.DirRecord rec in process.ActiveDirectory.GetChildren())
            {
                FileDescriptor fileC = process.Kernel.OpenAt(process.ActiveDirectory.FileDescriptor, rec.name, FileDescriptor.Flags.Create_Open, Permission.U_All, ref error);
                if (error == Filesystem.Error.None)
                if (fileC.Name == command[1])
                {
                    process.Kernel.Print(process, "File " + command[1] + " touched.");
                    return true;
                }
            }
            if (!activeDirectory.HasWritePermission(process.Credentials))
            {
                process.Kernel.Print(process, "Permission denied.");
                return true;
            }

            File file = process.Kernel.GetFileAt(activeDirectory, command[1], FileDescriptor.Flags.Read_Write | FileDescriptor.Flags.Create_Open, Permission.A_All, ref error);

            if(error != Filesystem.Error.None)
            {
                process.Kernel.Print(process, "Error " + error);
            }

            process.Kernel.Print(process, "File " + command[1]);
            return true;
        }

        public static bool Remove(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;

            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : rm [fileName]");
            }

            // TODO split/delimit this param command[1] because whitespace should really be escaped somehow.
            FileDescriptor fileDescriptor = process.Kernel.Open(process, command[1], FileDescriptor.Flags.Write, ref error);

            process.Kernel.UnlinkFile(fileDescriptor, ref error);

            if (error.Equals(Filesystem.Error.Permission_Denied))
            {
                process.Kernel.Print(process, "Permission denied.");
                return true;
            }

            if (error.Equals(Filesystem.Error.None))
            {
                process.Kernel.Print(process, "File " + command[1] + " removed.");
                return true;
            }

            process.Kernel.Print(process, "File does not exist.");
            return true;
        }

        public static bool Link(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;
            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : ln [source] [target]");
            }
            string[] inputs = command[1].Split(' ');
            if (inputs.Length < 2)
            {
                process.Kernel.Print(process, "Usage : ln [source] [target]");
            }
            var activeDirectory = process.ActiveDirectory;

            File source = process.ActiveDirectory.GetFile(inputs[0], FileDescriptor.Flags.None);

            if (source != null)
            {
                process.Kernel.Print(process, "File " + inputs[0] + " linked to " + command[1]);
                process.Kernel.LinkFile(process.ActiveDirectory.FileDescriptor, source.FileDescriptor, inputs[1], ref error);
                return true;
            } else
            {
                process.Kernel.Print(process, "Target File does not exist.");
            }

            return true;
        }

        public static bool MkDir(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;

            if (command.Length < 2)
            {
                process.Kernel.Print(process, "Usage : mkdir [folderName]");
                return true;
            }

            var activeDirectory = process.ActiveDirectory;

            bool passed = activeDirectory.HasWritePermission(process.Credentials);

            if (!passed)
            {
                process.Kernel.Print(process, "Permission denied.");
                return true;
            }

            File file = process.Kernel.Mkdir(activeDirectory, command[1], Permission.A_All & ~Permission.O_All, ref error);

            return true;
        }

        public static bool AddToNetMap(CommandProcess process, string[] commandUnsplit)
        {
            List<string> command = new List<string>();
            command.Add("netmap");
            command.AddRange(commandUnsplit[1].Split());
            if (command.Count < 4)
            {
                process.Kernel.Print(process, "Usage: netmap [ip] [x] [y]");
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
                    process.Kernel.Print(process, "Usage: music [(nameofsong) (Note: Must be in a folder called \"HNMPMusic\" in the Mods folder as an .wav file.)]\nOR music shuffle\nOR music list");
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
