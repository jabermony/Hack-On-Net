using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Daemons;
using HackLinks_Server.Daemons.Types.Http;
using HackLinks_Server.Files;
using System;
using System.Collections.Generic;

namespace HackLinks_Server.Computers.Processes.Daemons
{
    class HTTPDaemon : Daemon
    {
        public WebPage defaultPage;

        public string websiteName;

        public List<WebPage> webPages = new List<WebPage>();

        public Dictionary<Session, HTTPClient> httpSessions = new Dictionary<Session, HTTPClient>();

        protected override Type ClientType => typeof(HTTPClient);


        public HTTPDaemon(int pid, Node computer, Credentials credentials) : base(pid, computer, credentials)
        {

        }

        public WebPage GetPage(string v)
        {
            foreach (WebPage page in webPages)
                if (page.title == v)
                    return page;
            return null;
        }

        public override string StrType => "http";

        public override void OnStartUp()
        {
            base.OnStartUp();
            LoadWebPages();
        }

        public override void OnConnect(Session connectSession, DaemonClient client)
        {
            base.OnConnect(connectSession, client);
            httpSessions.Add(connectSession, (HTTPClient)client);
            ((HTTPClient)client).SetActivePage(defaultPage);
        }

        public override void OnDisconnect(Session disconnectSession)
        {
            base.OnDisconnect(disconnectSession);

            httpSessions.Remove(disconnectSession);
        }

        public override string GetSSHDisplayName()
        {
            return "Open Website";
        }

        public void LoadWebPages()
        {
            Filesystem.Error error = Filesystem.Error.None;
            File www = node.Kernel.GetFile(this, "/www", FileDescriptor.Flags.Read_Write, ref error);
            if (www == null || !www.Type.Equals(FileType.Directory))
                return;
            foreach(Util.FileUtil.DirRecord rec in www.GetChildren())
            {
                File file = www.GetFile(rec.name, FileDescriptor.Flags.Read);
                string content = file.GetContentString();
                if (content == null)
                    continue;
                WebPage newPage = WebPage.ParseFromFile(file);
                if (newPage == null)
                    return;
                webPages.Add(newPage);
                if(newPage.title == "index")
                {
                    this.defaultPage = newPage;
                }
            }
        }
    }
}
