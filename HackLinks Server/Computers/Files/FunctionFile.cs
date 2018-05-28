using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Computers.Files
{
    public class FunctionFile : File
    {
        public delegate string Read();
        public delegate void Write(string data);

        private Read input;
        private Write output;

        public override string Content { get => input(); set => output(value); }
        public override int Checksum => 0;

        public FunctionFile(int id, Node computer, File parent, string name, Read input, Write output) : base(id, computer, parent, name)
        {
            this.input = input;
            this.output = output;
        }
    }
}
