﻿using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    class HASH : CommandProcess
    {
        private static SortedDictionary<string, Tuple<string, Command>> commands = new SortedDictionary<string, Tuple<string, Command>>()
        {
            { "cd", new Tuple<string, Command>("cd [dir]\n    Moves current working directory to the specified directory.", ChangeDirectory) },
            { "daemon", new Tuple<string, Command>("daemon [daemon name]\n    If it's available we'll launch the given daemon.", Daemon) },
        };

        public override SortedDictionary<string, Tuple<string, Command>> Commands => commands;

        public HASH(int pid, Printer printer, Node computer, Credentials credentials) : base(pid,  printer, computer, credentials)
        {
            // left empty because we don't do anything special to initalize this Process
        }

        public bool HandleBuiltin(string command)
        {
            return RunCommand(command);
        }

        private bool HandleExternal(string command)
        {
            string[] commandParts = command.Split(new char[] { ' ' }, 2);
            File applicationFile = SearchPath(commandParts[0]);
            if (applicationFile != null)
            {
                Process child = computer.Kernel.StartProcess(this, applicationFile);
                child.Run(command);
                return true;
            }
            return false;
        }

        private File SearchPath(string v)
        {
            File file = ActiveDirectory.GetFile(v);
            if(file != null)
            {
                return file;
            }
            File bin = computer.fileSystem.rootFile.GetFile("bin");
            if(bin != null)
            {
                file = bin.GetFile(v);
            }
            return file;
        }

        public override void WriteInput(string inputData)
        {
            if(inputData != null && inputData.Length > 0)
            {
                if (HandleBuiltin(inputData))
                {
                    return;
                }
                if (HandleExternal(inputData))
                {
                    return;
                }
                Print($"{inputData.Split(new char[] { ' ' }, 2)[0]}: command not found");
            }
        }

        private static bool Daemon(CommandProcess process, string[] command)
        {
            if (command.Length != 2)
            {
                process.Print("Usage : daemon [name of daemon]");
                return true;
            }
            string target = command[1];

            process.computer.Kernel.OpenDaemon(process, target);

            return true;
        }

        public static bool ChangeDirectory(CommandProcess process, string[] command)
        {
            if (command.Length < 2)
            {
                process.Print("Usage : cd [folder]");
                return true;
            }
            if (command[1] == "..")
            {
                if (process.ActiveDirectory.Parent != null)
                {
                    process.ActiveDirectory = process.ActiveDirectory.Parent;
                    return true;
                }
                else
                {
                    process.Print("Invalid operation.");
                    return true;
                }
            }
            foreach (var file in process.ActiveDirectory.children)
            {
                if (file.Name == command[1])
                {
                    if (!file.Type.Equals(File.FileType.Directory))
                    {
                        process.Print("You cannot change active directory to a file.");
                        return true;
                    }
                    if (!file.HasExecutePermission(process.Credentials))
                    {
                        process.Print("You do not have permission to do this. You must have execute permission to access a directory.");
                        return true;
                    }
                    process.ActiveDirectory = file;
                    process.computer.Kernel.CD(process, file.Name);
                    return true;
                }
            }
            process.Print("No such folder.");
            return true;
        }
    }
}
