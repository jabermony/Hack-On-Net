using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Util
{
    public static class PathUtil
    {
        /// <summary>
        /// Returns the top level filename from the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string Basename(string path)
        {
            string[] parts = path.Split('/');
            int basenameIndex = parts.Length - 1;
            while(parts[basenameIndex].Length == 0 && basenameIndex > 0)
            {
                basenameIndex--;
            }
            return parts[basenameIndex];
        }

        public static string Normalize(string rawPath, string currentDirectory)
        {
            StringBuilder pathBuilder = new StringBuilder();
            string left = rawPath;
            int pos = 0;
            int tokenStart = 0;
            if (rawPath.StartsWith("/"))
            {
                if(rawPath.Length == 1)
                {
                    return "/";
                }
                pos = 1;
                left = left.Substring(pos);
                pathBuilder.Append("/");
            } else
            {
                pathBuilder.Append(currentDirectory);
                if(!currentDirectory.EndsWith("/"))
                {
                    pathBuilder.Append("/");
                }
            }
            while(pos < left.Length)
            {
                if (left[pos].Equals("."))
                {
                    tokenStart = pos;
                    pos++;
                } else if(left[pos].Equals("/") && pos - tokenStart > 1)
                {
                    // Fix for trailing '/' breaking path comparision
                    pathBuilder.Append(left.Substring(tokenStart, pos - tokenStart));
                    pos++;
                    tokenStart = pos;
                } else if (pos == left.Length - 1)
                {
                    pathBuilder.Append(left.Substring(tokenStart, (pos + 1) - tokenStart));
                    break;
                } else
                {
                    pos++;
                }
                
            }

            return pathBuilder.ToString();
        }

        /// <summary>
        /// Returns the path to the parent of the file specified by the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string Dirname(string path)
        {
            return path.Substring(0, GetFilenameIndex(path));
        }

        /// <summary>
        /// Return the index of the top level file on the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static int GetFilenameIndex(string path)
        {
            int position = path.LastIndexOf('/');
            while ((position == path.Length - 1 || path[position + 1] == '/') && position > 0)
            {
                position = path.LastIndexOf('/', position - 1);
            }
            return position;
        }
    }
}
