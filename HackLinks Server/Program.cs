﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using System.Diagnostics;
using System.IO;
using HackLinks_Server.Util;

namespace HackLinks_Server
{
    class Program
    {

        public static bool recieving = false;

        static void Main(string[] args)
        {
            Logger.LogFile = $".{Path.DirectorySeparatorChar}HackLinks.log";
            Logger.Archive = $".{Path.DirectorySeparatorChar}Archive";

            Logger.Info("HackLinks Server is starting up.");

            bool showHelp = false;
            bool rebuildDB = false;
            bool writeConfig = false;
            bool overwriteConfig = false;
            string writeConfigPath = null;

            //Set Defaults and create config object
            ConfigUtil.ConfigData configData = new ConfigUtil.ConfigData();
            configData.MySQLServer = "127.0.0.1";
            configData.Port = 27015;
            configData.Database = "hacklinks";
            configData.UserID = "root";
            configData.Password = "";
            configData.SaveFrequency = 300; // 300 seconds, 60 seconds * 5, 5 minutes.

            bool passSet = true;

            //Handle Args
            OptionSet options = new OptionSet() {
                { "s|server=", "the MySQL {SERVER} to use (default: \"127.0.0.1\").", v => configData.MySQLServer = v},
                { "d|database=", "the {DATABASE} to use (default: \"hacklinks\").", v => configData.Database = v},
                { "f|save-frequency=",  "the time to wait, in seconds, between writing the game state to the database (default: 300).", v => configData.SaveFrequency = int.Parse(v) },
                { "u|user=", "the {USERNAME} to connect with (default: \"root\").", v => configData.UserID = v},
                { "p|password:", "set the {PASSWORD} to connect with (default: None) or prompt for a password.", v => {passSet = v != null;  configData.Password = v;} },
                { "P|port=",
                    "set the {PORT} to open the server on (default: 27015).",
                    v =>
                    {
                        int result;
                        if(int.TryParse(v, out result))
                        {
                            configData.Port = result;
                        }
                        else
                        {
                            Logger.Warning("Invalid Port Specified: \"{0}\". Using Default Port.", v);
                        }
                    }
                },
                { "c|config:",
                    "load settings from {CONFIG} file (default: No) or from default config file \"serverconfig.conf\".\n" +
                    "If the file doesn't exist it will be created with the the final values when the server runs unless the {-o/--overwrite-config} flag specifies a file instead.",
                    v =>
                    {
                        //Use given path, Existing path if it exists, or default if not.
                        string readConfigPath = v ?? "serverconfig.conf";

                        //If we aren't overwritting or if we are but the path is unset
                        if(!overwriteConfig || overwriteConfig && writeConfigPath == null)
                            writeConfigPath = readConfigPath;

                        //Loadconfig returns true if the file was loaded. So we should write the config if it's not
                        writeConfig = ! ConfigUtil.LoadConfig(readConfigPath, configData);
                    }
                },
                { "o|overwrite-config:",
                    "force the {CONFIG} file to be overwritten with the final values when the server runs.\n" +
                    "You can optionally specify the config file to be written here.",
                    v => 
                    {
                        //If config path is specified use that instead
                        writeConfigPath = v ?? writeConfigPath ?? "serverconfig.conf";

                        overwriteConfig = true;
                    }
                },
                { "r|rebuild",  "rebuild the database (WARNING: this will delete all data).", v => rebuildDB = v != null },
                { "h|help",  "show this message and exit.", v => showHelp = v != null },
            };

            try
            {
                options.Parse(args);

                if (showHelp) // If help requested then show help and exit
                {
                    Console.WriteLine("Usage: HackLinks Server.exe [OPTIONS]");
                    Console.WriteLine("Starts the HackLinks Server.");
                    Console.WriteLine();

                    // output the options
                    Console.WriteLine("Options:");
                    options.WriteOptionDescriptions(Console.Out);

                    return;
                }

                //We write our config here as we likely don't want to save the prompted password
                if (overwriteConfig || writeConfig)
                    ConfigUtil.SaveConfig(writeConfigPath, configData);

                //Check if password is null and prompt if it is
                //Done here to avoid asking for password when other options are going to fail anyway or help should be displayed.
                if (!passSet)
                    configData.Password = GetPassword();

            } catch(OptionException e)
            {
                //One of our options failed. Output the message
                Logger.Exception(e);
                return;
            }

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, configData.Port);
          
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            Server.Instance.Initalize(configData);
            //If we're going to rebuild the DB we need to do it before data is loaded but after the server has the mysql config
            if (rebuildDB)
            {
                Server.Instance.DatabaseLink.RebuildDatabase();
            }
            Server.Instance.StartServer();

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                var stopWatch = Stopwatch.StartNew();
                while (true)
                {

                    if(recieving == false)
                    {
                        listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            listener);
                        Logger.Status($"Server ready, listening on {localEndPoint}");
                        recieving = true;
                    }
                  
                    double dT = stopWatch.ElapsedMilliseconds / (double)1000;
                    stopWatch.Restart();
                    Server.Instance.MainLoop(dT);
                }

            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private static string GetPassword()
        {
            Console.Write("Please Enter Password:");

            string password = string.Empty;

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            while (keyInfo.Key != ConsoleKey.Enter)
            {
                password += keyInfo.KeyChar;
                keyInfo = Console.ReadKey(true);
            }

            Console.WriteLine();

            return password;
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            recieving = false;

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            Server.Instance.AddClient(handler);
        }
    }
}
