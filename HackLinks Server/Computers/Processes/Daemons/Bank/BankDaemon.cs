using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Processes.Daemons.Bank;
using HackLinks_Server.Daemons;
using HackLinks_Server.Files;
using HackLinksCommon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HackLinksCommon.NetUtil;

namespace HackLinks_Server.Computers.Processes.Daemons
{
    class BankDaemon : Daemon
    {
        public override string StrType => "bank";

        protected override Type ClientType => typeof(BankClient);

        public override DaemonType GetDaemonType()
        {
            return DaemonType.BANK;
        }

        public BankDaemon(int pid, Node computer, Credentials credentials) : base(pid, computer, credentials)
        {

        }

        public List<BankAccount> accounts = new List<BankAccount>();

        public void LoadAccounts()
        {
            Filesystem.Error error = Filesystem.Error.None;
            accounts.Clear();
            File accountFile = Kernel.GetFile(this, "/bank/accounts.db", FileDescriptor.Flags.Read, Permission.None, ref error);
            if (accountFile == null)
                return;
            foreach (string line in accountFile.GetContentString().Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var data = line.Split(',');
                if (data.Length < 4)
                    continue;
                accounts.Add(new BankAccount(data[0], Convert.ToInt32(data[1]), data[2], data[3]));
            }
        }

        public void UpdateAccountDatabase()
        {
            Filesystem.Error error = Filesystem.Error.None;
            File accountFile = Kernel.GetFile(this, "/bank/accounts.db", FileDescriptor.Flags.None, Permission.None, ref error);
            if (accountFile == null)
                return;
            string newAccountsFile = "";
            foreach (var account in accounts)
            {
                newAccountsFile += account.accountName + "," + 0 + "," + account.password + "," + account.clientUsername + "\r\n";
            }
            accountFile.SetContent(newAccountsFile);
        }

        public bool CheckFolders(CommandProcess process)
        {
            Filesystem.Error error = Filesystem.Error.None;
            var bankFolder = Kernel.GetFile(process, "/bank", FileDescriptor.Flags.Read, Permission.None, ref error);
            if (bankFolder == null || !bankFolder.Type.Equals(FileType.Directory))
            {
                process.Kernel.Print(process, "No bank daemon folder was found ! (Contact the admin of this node to create one as the bank is useless without one)");
                return false;
            }
            var accountFile = Kernel.GetFile(process, "/bank/accounts.db", FileDescriptor.Flags.None, Permission.None, ref error);
            if (accountFile == null)
            {
                process.Kernel.Print(process, "No accounts file was found ! (Contact the admin of this node to create one as the bank is useless without one)");
                return false;
            }

            return true;
        }

        public void ProcessBankTransfer(BankAccount from, BankAccount to, string ip, int amount, Session session)
        {
            BankAccount account = null;
            foreach (var account2 in accounts)
            {
                if (account2 == to)
                {
                    account = account2;
                }
            }
            account.balance += amount;
            UpdateAccountDatabase();
            LogTransaction($"{to.accountName},Received {amount} from {from.accountName}@{ip} to {to.accountName}", session.sessionId, session.owner.homeComputer.ip);
        }

        public void LogTransaction(string transactionMessage, int sessionId, string ip)
        {
            Filesystem.Error error = Filesystem.Error.None;
            File transactionLog = Kernel.GetFile(this, "/bank/transactionlog.db", FileDescriptor.Flags.None, Permission.None, ref error);
            if (transactionLog != null)
            {
                if (transactionLog.GetContentString() == "")
                    transactionLog.SetContent(transactionLog.GetContentString().Insert(0, transactionMessage));
                else
                    transactionLog.SetContent(transactionLog.GetContentString().Insert(0, transactionMessage + "\r\n"));
            }
            // TODO logging
            //node.Log(Log.LogEvents.BankTransaction, transactionMessage, sessionId, ip);
        }

        public override void OnStartUp()
        {
            LoadAccounts();
        }

        public override string GetSSHDisplayName()
        {
            return null;
        }
    }
}
