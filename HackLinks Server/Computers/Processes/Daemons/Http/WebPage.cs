﻿using HackLinks_Server.Daemons.Types.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HackLinksCommon.NetUtil;
using System.Text.RegularExpressions;
using HackLinks_Server.Files;
using HackLinks_Server.Computers.Processes;

namespace HackLinks_Server.Daemons.Types.Http
{
    class WebPage
    {
        public string content;
        public string title;

        Dictionary<string, WebInterface> interfaces = new Dictionary<string, WebInterface>();

        public void SendWebPage(Session session)
        {
            var commandData = new List<string>() { "state", "http", "page", title, content};

            session.owner.Send(PacketType.KERNL, commandData.ToArray());
        }

        public static WebPage ParseFromFile(File file)
        {
            WebPage page = new WebPage();
            page.title = file.Name;
            page.content = file.GetContentString();
            MatchCollection matches = Regex.Matches(file.GetContentString(), "(<!interface.*>.*<\\/interface>)",RegexOptions.Multiline);
            foreach(Match match in matches)
            {
                var newInterface = WebInterface.ParseFromTag(match.Value, file);
                if (newInterface == null)
                    continue;
                page.content = page.content.Replace(match.Value, newInterface.GetClientDisplay());
                page.interfaces.Add(newInterface.ID, newInterface);
            }
            return page;
        }

        public void UseInterfaces(HTTPClient httpSession, string[] arguments)
        {
            foreach(KeyValuePair<string, WebInterface> webInt in interfaces)
            {
                if (webInt.Key != arguments[0])
                    continue;
                webInt.Value.Use(httpSession, arguments);
                break;
            }
        }
    }
}
