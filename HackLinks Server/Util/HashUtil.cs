using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Util
{
    public static class HashUtil
    {
        private static uint m = 0x5bd1e995;
        private static int r = 24;
        private static uint seed = 0x9747b28c;

        public static uint CalcMurmur(Stream bytes)
        {
            long length = bytes.Length;
            if (length == 0)
                return 0;
            uint hash = seed ^ (uint)length;
            while (length >= 4)
            {
                uint k = (uint)(bytes.ReadByte() | bytes.ReadByte() << 8 | bytes.ReadByte() << 16 | bytes.ReadByte() << 24);
                k *= m;
                k ^= k >> r;
                k *= m;

                hash *= m;
                hash ^= k;
                length -= 4;
            }
            switch (length)
            {
                case 3:
                    hash ^= (ushort)(bytes.ReadByte() | bytes.ReadByte() << 8);
                    hash ^= (uint)(bytes.ReadByte() << 16);
                    hash *= m;
                    break;
                case 2:
                    hash ^= (ushort)(bytes.ReadByte() | bytes.ReadByte() << 8);
                    hash *= m;
                    break;
                case 1:
                    hash ^= (byte)bytes.ReadByte();
                    hash *= m;
                    break;
            }

            hash ^= hash >> 13;
            hash += m;
            hash ^= hash >> 15;

            return hash;
        }

        public static uint CalcMurmur(IList<byte> bytes)
        {
            int length = bytes.Count;
            if (bytes.Count == 0)
                return 0;
            uint hash = seed ^ (uint) length;
            int currentIndex = 0;
            while(length >= 4)
            {
                uint k = (uint)(bytes[currentIndex++] | bytes[currentIndex++] << 8 | bytes[currentIndex++] << 16 | bytes[currentIndex++] << 24);
                k *= m;
                k ^= k >> r;
                k *= m;

                hash *= m;
                hash ^= k;
                length -= 4;
            }
            switch (length)
            {
                case 3:
                    hash ^= (ushort)(bytes[currentIndex++] | bytes[currentIndex++] << 8);
                    hash ^= (uint)(bytes[currentIndex++] << 16);
                    hash *= m;
                    break;
                case 2:
                    hash ^= (ushort)(bytes[currentIndex++] | bytes[currentIndex++] << 8);
                    hash *= m;
                    break;
                case 1:
                    hash ^= bytes[currentIndex++];
                    hash *= m;
                    break;
            }

            hash ^= hash >> 13;
            hash += m;
            hash ^= hash >> 15;

            return hash;
        }

        public static uint CalcMurmur(string input)
        {
            return CalcMurmur(Encoding.UTF8.GetBytes(input));
        }
    }
}
