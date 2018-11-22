using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Daemons;
using HackLinks_Server.Computers.Processes.Daemons;
using HackLinks_Server.Computers.Processes.Daemons.Bank;
using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Processes
{
    class BankClient : DaemonClient
    {
        public SortedDictionary<string, Tuple<string, Command>> commands = new SortedDictionary<string, Tuple<string, Command>>()
        {
            { "account", new Tuple<string, Command>("account [create/login/resetpass/balance/transfer/transactions/close]\n    Performs an account operation.", BankAccount) },
            { "balance", new Tuple<string, Command>("balance set [accountname] [value]/get [accountname]\n    Sets or gets balance (DEBUG COMMAND)", Balance) }
        };

        public override SortedDictionary<string, Tuple<string, Command>> Commands => commands;
        private BankAccount loggedInAccount = null;

        public BankClient(Session session, Daemon daemon, int pid, Node computer, Credentials credentials) : base(session, daemon, pid, computer, credentials)
        {
            
        }

        public override bool RunCommand(string command)
        {
            // We hide the old runCommand function to perform this check on startup
            if (!((BankDaemon)Daemon).CheckFolders(this))
            {
                return true;
            }
            return base.RunCommand(command);
        }

        public static bool BankAccount(CommandProcess process, string[] command)
        {
            Filesystem.Error error = Filesystem.Error.None;
            BankClient client = (BankClient)process;
            BankDaemon daemon = (BankDaemon)client.Daemon;

            File accountFile = process.Kernel.GetFile(process, "/bank/accounts.db", FileDescriptor.Flags.Read, Permission.None, ref error);

            if (command[0] == "account")
            {
                if (command.Length < 2)
                {
                    process.Kernel.Print(process, "Usage : account [create/login/resetpass/balance/transfer/transactions/close]");
                    return true;
                }
                // TODO: Implement Transaction Log
                var cmdArgs = command[1].Split(' ');
                if (cmdArgs[0] == "create")
                {
                    // TODO: When mail daemon is implemented, require an email address for password reset
                    if (cmdArgs.Length < 3)
                    {
                        process.Kernel.Print(process, "Usage : account create [accountname] [password]");
                        return true;
                    }
                    List<string> accounts = new List<string>();
                    var accountsFileContent = accountFile.GetContentString().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (accountsFileContent.Length != 0)
                    {
                        foreach (string line in accountFile.GetContentString().Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var data = line.Split(',');
                            if (data.Length < 4)
                                continue;
                            accounts.Add(data[0]);
                        }
                    }
                    if (accounts.Contains(cmdArgs[1]))
                    {
                        process.Kernel.Print(process, "This account name is not available");
                        return true;
                    }
                    daemon.accounts.Add(new BankAccount(cmdArgs[1], 0, cmdArgs[2], client.Session.owner.username));
                    daemon.UpdateAccountDatabase();
                    process.Kernel.Print(process, "Your account has been opened. Use account login [accountname] [password] to login.");
                }
                if (cmdArgs[0] == "login")
                {
                    if (cmdArgs.Length < 3)
                    {
                        process.Kernel.Print(process, "Usage : account login [accountname] [password]");
                        return true;
                    }
                    foreach (var account in daemon.accounts)
                    {
                        if (account.accountName == cmdArgs[1] && account.password == cmdArgs[2])
                        {
                            client.loggedInAccount = account;
                            // TODO logging
                            //daemon.computer.Log(Log.LogEvents.Login, daemon.computer.logs.Count + 1 + " " + client.Session.owner.homeComputer.ip + " logged in as bank account " + account.accountName, client.Session.sessionId, client.Session.owner.homeComputer.ip);
                            process.Kernel.Print(process, $"Logged into bank account {account.accountName} successfully");
                            return true;
                        }
                    }
                    process.Kernel.Print(process, "Invalid account name or password");
                }
                if (cmdArgs[0] == "resetpass")
                {
                    if (cmdArgs.Length < 3)
                    {
                        process.Kernel.Print(process, "Usage : account resetpass [accountname] [newpassword]");
                        return true;
                    }
                    // TODO: When mail daemon is implemented, change it to verify using email so players can hack by password reset
                    foreach (var account in daemon.accounts)
                    {
                        if (account.accountName == cmdArgs[1])
                        {
                            if (account.clientUsername == client.Session.owner.username)
                            {
                                account.password = cmdArgs[2];
                                daemon.UpdateAccountDatabase();
                                process.Kernel.Print(process, "Your password has been changed");
                            }
                            else
                                process.Kernel.Print(process, "You are not the owner of the account");
                            break;
                        }
                    }
                    return true;
                }
                if (cmdArgs[0] == "balance")
                {
                    if (client.loggedInAccount == null)
                    {
                        process.Kernel.Print(process, "You are not logged in");
                        return true;
                    }
                    process.Kernel.Print(process, $"Account balance for {client.loggedInAccount.accountName} is {client.loggedInAccount.balance}");
                }
                if (cmdArgs[0] == "transfer")
                {
                    if (cmdArgs.Length < 4)
                    {
                        process.Kernel.Print(process, "Usage : account transfer [receivingaccountname] [receivingbankip] [amount]");
                        return true;
                    }
                    if (client.loggedInAccount == null)
                    {
                        process.Kernel.Print(process, "You are not logged in");
                        return true;
                    }
                    if (client.loggedInAccount.balance < Convert.ToInt32(cmdArgs[3]))
                    {
                        process.Kernel.Print(process, "Account does not have enough balance");
                        return true;
                    }
                    if (Server.Instance.GetComputerManager().GetNodeByIp(cmdArgs[2]) == null)
                    {
                        process.Kernel.Print(process, "The receiving computer does not exist");
                        return true;
                    }
                    BankDaemon targetBank = null;
                    foreach (var computer in Server.Instance.GetComputerManager().NodeList)
                    {
                        if (computer.ip == cmdArgs[2])
                        {
                            Daemon targetDaemon = computer.GetDaemon("Bank");
                            if (targetDaemon == null)
                            {
                                process.Kernel.Print(process, "The receiving computer does not have a bank daemon");
                                return true;
                            }
                            targetBank = (BankDaemon)targetDaemon;
                            break;
                        }
                    }
                    BankAccount accountTo = null;
                    foreach (var account in targetBank.accounts)
                    {
                        if (account.accountName == cmdArgs[1])
                        {
                            accountTo = account;
                            break;
                        }
                    }
                    if (accountTo == null)
                    {
                        process.Kernel.Print(process, "The receiving account does not exist");
                        return true;
                    }                    
                    targetBank.ProcessBankTransfer(client.loggedInAccount, accountTo, cmdArgs[2], int.Parse(cmdArgs[3]), client.Session);
                    daemon.LogTransaction($"{client.loggedInAccount.accountName},{client.Session.owner.homeComputer.ip} transferred {cmdArgs[3]} from {client.loggedInAccount.accountName} to {accountTo.accountName}@{cmdArgs[2]}", client.Session.sessionId, client.Session.owner.homeComputer.ip);
                }
                if (cmdArgs[0] == "transactions")
                {
                    if (client.loggedInAccount == null)
                    {
                        process.Kernel.Print(process, "You are not logged in");
                        return true;
                    }
                    File transactionLog = process.Kernel.GetFile(process, "/bank/transactionlog.db", FileDescriptor.Flags.Read, Permission.None, ref error);
                    if (transactionLog == null)
                    {
                        process.Kernel.Print(process, "This bank does not keep transaction logs");
                        return true;
                    }
                    string[] transactions = transactionLog.GetContentString().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (transactions.Length == 0)
                    {
                        process.Kernel.Print(process, "The transaction log database is empty");
                        return true;
                    }
                    string transactionLogForClient = "";
                    foreach (var transactionUnsplit in transactions)
                    {
                        string[] transaction = transactionUnsplit.Split(',');
                        if (transaction[0] == client.loggedInAccount.accountName)
                            transactionLogForClient += transaction[1] + "\n";
                    }
                    if (transactionLogForClient == "")
                        transactionLogForClient += "Your transaction log is empty";
                    File transactionFileForClient = client.Session.owner.homeComputer.Kernel.GetFile(process, "/Bank_Transaction_Log_For_" + client.loggedInAccount.accountName, FileDescriptor.Flags.Read_Write | FileDescriptor.Flags.Create_Open, Permission.O_All | Permission.G_All, ref error);
                    transactionFileForClient.SetContent(transactionLogForClient);
                    process.Kernel.Print(process, "A file containing your transaction log has been uploaded to your computer");
                }
                if (cmdArgs[0] == "close")
                {
                    if (client.loggedInAccount == null)
                    {
                        process.Kernel.Print(process, "You are not logged in");
                        return true;
                    }
                    if (client.loggedInAccount.balance != 0)
                    {
                        process.Kernel.Print(process, "Your account balance must be zero before you can close your account.\nUse account transfer to transfer your money out of your account");
                        return true;
                    }
                    daemon.accounts.Remove(client.loggedInAccount);
                    daemon.UpdateAccountDatabase();
                    client.loggedInAccount = null;
                    process.Kernel.Print(process, "Your account has been closed");
                }
                return true;
            }
            return false;
        }

        public static bool Balance(CommandProcess process, string[] command)
        {
            BankClient client = (BankClient)process;
            BankDaemon daemon = (BankDaemon)client.Daemon;
            Filesystem.Error error = Filesystem.Error.None;

            if (command[0] == "balance")
            {
                if (command.Length < 2)
                {
                    process.Kernel.Print(process, "Usage : balance set [accountname] [value]/get [accountname]");
                    return true;
                }
                var cmdArgs = command[1].Split(' ');
                if (cmdArgs.Length < 2)
                {
                    process.Kernel.Print(process, "Usage : balance set [accountname] [value]/get [accountname]");
                    return true;
                }
                if (cmdArgs[0] == "set" && cmdArgs.Length < 3)
                {
                    process.Kernel.Print(process, "Usage : balance set [accountname] [value]/get [accountname]");
                    return true;
                }
                BankAccount account = null;
                foreach (var account2 in daemon.accounts)
                {
                    if (account2.accountName == cmdArgs[1])
                    {
                        account = account2;
                        break;
                    }
                }
                if (account == null)
                {
                    process.Kernel.Print(process, "Account data for this account does not exist in the database");
                    return true;
                }
                if (cmdArgs[0] == "set")
                {
                    if(int.TryParse(cmdArgs[2], out int val))
                    {
                        account.balance = val;
                        daemon.UpdateAccountDatabase();
                        var bankFolder = process.Kernel.GetFile(process, "/bank", FileDescriptor.Flags.Read, Permission.None, ref error);
                        daemon.LogTransaction($"{account.accountName},CHEATED Balance set to {val}", client.Session.sessionId, client.Session.owner.homeComputer.ip);
                    }
                    else
                    {
                        process.Kernel.Print(process, "Error: non-integer value specified");
                        return true;
                    }
                }
                process.Kernel.Print(process, $"Account balance for {account.accountName} is {account.balance}");
                return true;
            }
            return false;
        }
    }
}
