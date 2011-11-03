using System;
using System.Collections.Generic;
using System.Text;

namespace m2net
{
    /// <summary>
    /// Replacements for string functions that operate on bytes instead chars.
    /// </summary>
    public static class ByteArrayExtensions
    {
        public static ArraySegment<T> Substring<T>(this ArraySegment<T> data, int startIndex)
        {
            return new ArraySegment<T>(data.Array, data.Offset + startIndex, data.Count - startIndex);
        }

        public static ArraySegment<T> Substring<T>(this ArraySegment<T> data, int startIndex, int length)
        {
            return new ArraySegment<T>(data.Array, data.Offset + startIndex, length);
        }

        public static ArraySegment<T> Substring<T>(T[] data, int startIndex, int length)
        {
            return new ArraySegment<T>(data, startIndex, length);
        }

        public static string ToAsciiString(this byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        public static string ToAsciiString(this ArraySegment<byte> bytes)
        {
            return Encoding.ASCII.GetString(bytes.Array, bytes.Offset, bytes.Count);
        }

        public static string ToString(this ArraySegment<byte> bytes, Encoding enc)
        {
            return enc.GetString(bytes.Array, bytes.Offset, bytes.Count);
        }

        public static byte[] ToArray(this ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Buffer.BlockCopy(seg.Array, seg.Offset, copy, 0, seg.Count);
            return copy;
        }

        public static byte Get(this ArraySegment<byte> seg, int index)
        {
            return seg.Array[seg.Offset + index];
        }

        public static List<ArraySegment<byte>> Split(this byte[] msg, char separator, int count)
        {
            return Split(new ArraySegment<byte>(msg), separator, count);
        }

        public static List<ArraySegment<byte>> Split(this ArraySegment<byte> msg, char separator, int count)
        {
            var ret = new List<ArraySegment<byte>>();
            int i = 0;
            for (int substringNdx = 1; substringNdx < count; substringNdx++)
            {
                if (i == msg.Count)
                    break;

                int splitCount = 0;
                while ((i + splitCount) < msg.Count && msg.Get(i + splitCount) != (byte)separator)
                    splitCount++;

                ret.Add(Substring(msg, i, splitCount));
                i += splitCount + 1;
            }

            if (i != msg.Count)
                ret.Add(Substring(msg, i, msg.Count - i));

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
        public static ArraySegment<byte>[] ParseNetstring(this ArraySegment<byte> msg)
        {
            var split = msg.Split(':', 2);
            int len = int.Parse(split[0].ToAsciiString());
            if (split[1].Get(len) != ',')
                throw new FormatException("Netstring did not end in ','.");
            return new ArraySegment<byte>[] { Substring(split[1], 0, len), Substring(split[1], len + 1, split[1].Count - len - 1) };
        }

        public static ArraySegment<byte>[] ParseNetstring(this byte[] msg)
        {
            return ParseNetstring(new ArraySegment<byte>(msg));
        }
    }
}
