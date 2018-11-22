using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Computers.Filesystems;
using File = HackLinks_Server.Files.File;
using HackLinks_Server.Util;

namespace HackLinks_Server.Computers.Permissions
{
    public class Account
    {
        public string Username {get; set; }
        public string Password {get; set; }
        public int UserId { get; set; } = -1;
        public int GroupId {get; set; }
        public string Info { get; set; }

        public string HomeString;

        public string DPString;
        
        public Kernel Kernel { get; }

        public Account(string line, Kernel kernel)
        {
            string[] accountData = line.Split(':');

            Username = accountData[0];
            Password = accountData[1];
            UserId = Convert.ToInt32(accountData[2]);
            GroupId = Convert.ToInt32(accountData[3]);
            Info = accountData[4];
            HomeString = accountData[5];
            DPString = accountData[6];
            Kernel = kernel;
        }

        public Account( string username, string password, int groupId,string info,string homeString, string dpString, Kernel kernel)
        {
            HomeString = homeString;
            DPString = dpString;
            Username = username;
            Password = password;
            GroupId = groupId;
            Info = info;
            Kernel = kernel;
        }

        public Account(string username, string password,Group group,string info,string homeString, string dpString, Kernel kernel)
        {
            Username = username;
            Password = password;
            GroupId = (int) group;
            Info = info;
            HomeString = homeString;
            DPString = dpString;
            Kernel = kernel;
        }

        public Account(string username, string password, int groupId,string info, File homeDirectory, File defaultProcess, Kernel kernel)
        {
            Username = username;
            Password = password;
            GroupId = groupId;
            Info = info;
            HomeString = homeDirectory.Name;
            DPString = defaultProcess.Name;
            Kernel = kernel;
        }

        public Account(string username, string password,Group group,string info, File homeDirectory, File defaultProcess, Kernel kernel)
        {
            Username = username;
            Password = password;
            GroupId = (int)group;
            Info = info;
            HomeString = homeDirectory.Name;
            DPString = defaultProcess.Name;
            Kernel = kernel;
        }

    public Group GetGroup()
        {
            switch (GroupId)
            {
                case 0:
                    return Group.ROOT;
                case 1:
                    return Group.ADMIN;
                case 2:
                    return Group.USER;
                case 3:
                    return Group.GUEST;
                default:
                    return Group.INVALID;
            }
        }

        public void ApplyChanges(Process process)
        {
            Filesystem.Error error = Filesystem.Error.None;
            File passwd = process.Kernel.GetFile(process, "/etc/passwd", FileDescriptor.Flags.Read_Write, ref error);
            if(error != Filesystem.Error.None)
            {
                Logger.Error("No passwd");
                return;
            }
            string[] accounts = passwd.GetContentString().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (UserId == -1)
            {
                if (new Account(accounts.Last(), Kernel).UserId < 1000) UserId = 1000;
                else UserId = new Account(accounts.Last(), Kernel).UserId + 1;
            }

            Account before = null;
            if (UserId == -3)
            {
                if (new Account(accounts.Last(), Kernel).UserId < 1000) UserId = new Account(accounts.Last(), Kernel).UserId + 1;
                else
                {
                    before = new Account(accounts.First(), Kernel);
                    foreach (string account in accounts)
                    {
                        if (new Account(account, Kernel).UserId < 1000)
                        {
                            before = new Account(account, Kernel);
                            continue;
                        }

                        UserId = before.UserId + 1;
                    }
                }
            }
            string line = Username + ":" + Password + ":" + UserId + ":" + GroupId + ":" + Info + ":" + HomeString + ":" + DPString;
            string acc = "";
            if (before != null)
            {
                foreach (string account in accounts)
                {
                    if (account.StartsWith(before.Username + ":"))
                    {
                        acc += "\r\n" + account;
                        acc += "\r\n" + line;
                    }
                    else if (account.StartsWith(Username + ":")) continue;
                    else acc += "\r\n" + account;
                }
            }
            else
            {
                foreach (string account in accounts)
                {

                    if (account.StartsWith(Username + ":"))
                    {
                        if (UserId == -2) continue;
                        acc += "\r\n" + line;
                    }
                    else acc += "\r\n" + account;
                }

                if (accounts.All(ac => !ac.StartsWith(Username + ":")))
                {
                    acc += "\r\n" + line;
                }
            }

            passwd.SetContent(acc);
        }

        public static List<Account> FromFile(Process process, File passwd,Node computer)
        {
            List<Account> tmp = new List<Account>();
            string[] accounts = passwd.GetContentString().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string account in accounts)
            {
                tmp.Add(new Account(account,computer.Kernel));
            }

            return tmp;
        }

        public void Delete()
        {
            UserId = -2;
        }

        public static Account FromId(int UID, Kernel kernel, Process process)
        {
            List<Account> accounts = kernel.GetAccounts(process);
            foreach (Account account in accounts)
            {
                if (account.UserId == UID) return account;
            }

            return null;
        }
    }
}