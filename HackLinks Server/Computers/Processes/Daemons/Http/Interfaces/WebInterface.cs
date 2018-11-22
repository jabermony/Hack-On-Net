using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Computers.Processes.Daemons.Http.Interfaces;
using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HackLinks_Server.Daemons.Types.Http.Interfaces
{
    class WebInterface
    {
        public string id;
        public string ID { get { return id; } }

        protected delegate WebInterface Factory(Dictionary<string, string> attributes);


        private static Dictionary<string, Factory> interfaceCreators =
            new Dictionary<string, Factory>()
            {
                { "default",  WebInterface.Instanciate},
                { "passwordSecurity", PasswordSecurity.Instanciate }
            };

        public virtual void Use(HTTPClient session, string[] args)
        {

        }

        public static WebInterface Instanciate(Dictionary<string, string> attributes)
        {
            return null;
        }

        public static WebInterface ParseFromTag(string value, File pageFile)
        {
            Dictionary<string, string> attrValues = new Dictionary<string, string>();
            MatchCollection attrVal = Regex.Matches(value, "(\\S+)=[\"']?((?:.(?![\"']?\\s + (?:\\S +)=|[> \"']))+.)[\"']?");
            foreach (Match match in attrVal)
            {
                string attrId = match.Groups[1].Value;
                string attrValue = match.Groups[2].Value;

                attrValues[attrId] = attrValue; // Does it crash ?
            }
            if (!attrValues.ContainsKey("file"))
                return null;
            var interfaceFileName = attrValues["file"];
            var interfaceFile = pageFile.Parent.GetFile(interfaceFileName, FileDescriptor.Flags.Read);
            if (interfaceFile == null)
                return null;
            if (interfaceFile.Type != FileType.Regular)
                return null;
            if (interfaceFile.HasExecutePermission(0, Computers.Permissions.Group.ROOT))
                return null;
            var lines = interfaceFile.GetContentString().Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
            string intId = lines[0];

            var newInterface = interfaceCreators[intId](attrValues);
            if (newInterface == null)
                return null;
            if (!attrValues.ContainsKey("id"))
                return null;
            newInterface.id = attrValues["id"];

            return newInterface;
        }

        public virtual string GetClientDisplay()
        {
            return "";
        }
    }
}
