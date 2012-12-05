using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace m2net
{
    public abstract class NetString
    {
        static readonly Encoding sLengthEncoder = Encoding.ASCII;

        /// <summary>
        /// The size of the net string including the length string and delimiters.
        /// </summary>
        /// <remarks>
        /// That is, the number of bytes that ToByteArray(byte[],int) will put
        /// into the given array.
        /// </remarks>
        public readonly int ByteCount;
        readonly string mLengthStr;

        public NetString(int byteCount)
        {
            mLengthStr = byteCount.ToString(CultureInfo.InvariantCulture);
            this.ByteCount = sLengthEncoder.GetByteCount(mLengthStr) + 2 + byteCount;
        }

        protected abstract void copyContentArray(byte[] array, int index);

        public void ToByteArray(byte[] bytes, int index)
        {
            int lastIndex = index + ByteCount - 1;

            sLengthEncoder.GetBytes(mLengthStr, 0, mLengthStr.Length, bytes, index);
            index += mLengthStr.Length;

            bytes[index] = (byte)':';
            index++;

            copyContentArray(bytes, index);
            bytes[lastIndex] = (byte)',';
        }

        public byte[] ToByteArray()
        {
            var ret = new byte[ByteCount];
            ToByteArray(ret, 0);
            return ret;
        }

        /* +----------------------------------------+
         * |            Static Creation             |
         * +----------------------------------------+ */

        public static NetString Create(string str, Encoding enc)
        {
            return new StringNetString(str, enc);
        }

        public static NetString Create(byte[] bytes)
        {
            return new ByteNetString(bytes);
        }

        public static NetString Concat(params NetString[] netStrings)
        {
            return new CompositeNetString(netStrings);
        }

        /* +----------------------------------------+
         * |             Sub Classes                |
         * +----------------------------------------+ */

        class CompositeNetString : NetString
        {
            //Callers could potentically mutate this array, maybe we should create our own copy?
            readonly NetString[] mStrings;

            public CompositeNetString(NetString[] netStrings)
                : base(netStrings.Sum(ns => ns.ByteCount))
            {
                mStrings = netStrings;
            }

            protected override void copyContentArray(byte[] array, int index)
            {
                for (int i = 0; i < mStrings.Length; i++)
                {
                    var ns = mStrings[i];
                    ns.ToByteArray(array, index);
                    index += ns.ByteCount;
                }
            }
        }

        class ByteNetString : NetString
        {
            readonly byte[] mBytes;
            public ByteNetString(byte[] bytes)
                : base(bytes.Length)
            {
                this.mBytes = bytes;
            }

            protected override void copyContentArray(byte[] array, int index)
            {
                Buffer.BlockCopy(mBytes, 0, array, index, mBytes.Length);
            }
        }

        class StringNetString : NetString
        {
            readonly string mStr;
            readonly Encoding mEnc;
            public StringNetString(string str, Encoding enc)
                : base(enc.GetByteCount(str))
            {
                mStr = str;
                mEnc = enc;
            }

            protected override void copyContentArray(byte[] array, int index)
            {
                //ignoring the return value of this for now,
                //might be useful to add a sanity check
                mEnc.GetBytes(mStr, 0, mStr.Length, array, index);
            }
        }
    }
}
