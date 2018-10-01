using System;
using System.Collections.Generic;

namespace HackLinks_Server
{
    public class CompiledFileManager
    {
        private Dictionary<uint, string> types = new Dictionary<uint, string>();

        public void AddType(uint checksum, string type)
        {
            types.Add(checksum, type);
        }

        public string GetType(uint checksum)
        {
            return types.ContainsKey(checksum) ? types[checksum] : "False";
        }

        public Dictionary<uint, string> GetMap()
        {
            // We return a shallow clone here to prevent unauthorized manipulation of the map
            return new Dictionary<uint, string>(types);
        }
    }
}