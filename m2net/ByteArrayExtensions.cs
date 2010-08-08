using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace m2net
{
    /// <summary>
    /// Replacements for string functions that operate on bytes instead chars.
    /// </summary>
    /// <remarks>
    /// Currently these functions are implemented using copying.  Idealy these
    /// would be implemented using a method that avoids copying.  But this
    /// is easier for now.
    /// </remarks>
    public static class ByteArrayExtensions
    {
        public static string ToAsciiString(this byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        public static List<byte[]> Split(this byte[] msg, char separator, int count)
        {
            var ret = new List<byte[]>();
            int i = 0;
            for (int substringNdx = 1; substringNdx < count; substringNdx++)
            {
                if (i == msg.Length)
                    break;

                int splitCount = 0;
                while ((i + splitCount) < msg.Length && msg[i + splitCount] != (byte)separator)
                    splitCount++;

                ret.Add(copy(msg, i, splitCount));
                i += splitCount + 1;
            }

            if (i != msg.Length)
                ret.Add(copy(msg, i, msg.Length - i));

            return ret;
        }

        /// <summary>
        /// Parses one net string and returns the rest of the bytes.
        /// </summary>
        /// <remarks>
        /// Based on parse_netstring in Mongrel2's request.py.
        /// </remarks>
        /// <param name="msg"></param>
        /// <returns>index 0 is the netstring, index 1 is the rest</returns>
        public static byte[][] ParseNetstring(this byte[] msg)
        {
            var split = msg.Split(':', 2);
            int len = int.Parse(Encoding.ASCII.GetString(split[0]));
            if (split[1][len] != ',')
                throw new FormatException("Netstring did not end in ','.");
            return new byte[][] { copy(split[1], 0, len), copy(split[1], len + 1, split[1].Length - len - 1) };
        }

        private static byte[] copy(byte[] msg, int ndx, int count)
        {
            byte[] copy = new byte[count];
            Buffer.BlockCopy(msg, ndx, copy, 0, count);
            return copy;
        }
    }
}
