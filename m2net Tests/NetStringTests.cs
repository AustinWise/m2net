using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using m2net;

namespace m2net_Tests
{
    [TestClass]
    public class NetStringTests
    {
        [TestMethod]
        public void JustStr()
        {
            testRoundTrip("test:::,,,", Encoding.ASCII);
            testRoundTrip("私はアニメが好きなです。", Encoding.UTF8);
        }

        private void testRoundTrip(string str, Encoding enc)
        {
            var ns = NetString.Create(str, enc);
            var bytes = ns.ToByteArray();

            var parsedBytes = bytes.ParseNetstring();
            var parsedStr = enc.GetString(parsedBytes[0].Array, parsedBytes[0].Offset, parsedBytes[0].Count);

            Assert.AreEqual(str, parsedStr);
        }

        [TestMethod]
        public void Concat()
        {
            var enc = Encoding.UTF8;
            var str1 = "turtles in the sky with diamonds";
            var str2 = "喫茶店に行きました。";

            var ns = NetString.Concat(NetString.Create(str1, enc), NetString.Create(str2, enc)).ToByteArray();
            var b = ns.ParseNetstring()[0].ParseNetstring();

            Assert.AreEqual(str1, b[0].ToString(enc));

            b = b[1].ParseNetstring();
            Assert.AreEqual(str2, b[0].ToString(enc));
        }

        [TestMethod]
        public void Bytes()
        {
            var bytes = Encoding.ASCII.GetBytes("turtle");

            var ns = NetString.Create(bytes).ToByteArray();
            var parsedBytes = ns.ParseNetstring()[0].ToArray();

            assertArraysEqual(bytes, parsedBytes);
        }

        void assertArraysEqual<T>(T[] expected, T[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
