using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Filesystems;
using HackLinks_Server.Computers.Processes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HackLinks_Server.Util
{
    public static class FileUtil
    {
        public struct DirRecord
        {
            public ulong filesystemId;
            public ulong inode;
            public string name;

            public DirRecord(ulong filesystemId, ulong inode, string name)
            {
                this.filesystemId = filesystemId;
                this.inode = inode;
                this.name = name;
            }
        }


        public static List<DirRecord> GetDirectoryList(Filesystem system, FileHandle file)
        {
            List<DirRecord> recs = new List<DirRecord>();
            if (system.GetFileType(file).Equals(FileType.Directory))
            {
                using (Stream fStream = new FilesystemStream(system, file))
                {
                    long length = fStream.Length;
                    while (fStream.Position < length)
                    {
                        DirRecord dirRecord = ReadDirRecord(fStream);
                        recs.Add(dirRecord);
                    }
                }

                return recs;
            }
            else
            {
                return recs;
            }
        }

        private static DirRecord ReadDirRecord(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            ulong filesystemId = reader.ReadUInt64();
            ulong inode = reader.ReadUInt64();
            string name = reader.ReadString();

            DirRecord record = new DirRecord(filesystemId, inode, name);
            return record;
        }

        public static byte[] FromDirRecords(params DirRecord[] recs)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);

            foreach(DirRecord record in recs)
            {
                writer.Write(record.filesystemId);
                writer.Write(record.inode);
                writer.Write(record.name);
            }

            return stream.ToArray();
        }

        public static string BuildFileListing(params DirRecord[] recs)
        {
            // we encode as a hex string so we can insert it into the DB
            return ByteArrayToHexString(FromDirRecords(recs));
        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            string HexChars = "0123456789ABCDEF";

            foreach (byte b in bytes)
            {
                result.Append(HexChars[(int)(b >> 4)]);
                result.Append(HexChars[(int)(b & 0xF)]);
            }

            return result.ToString();
        }
    }
}
