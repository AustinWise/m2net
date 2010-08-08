using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using m2net;

namespace m2net_Tests
{
    [TestClass]
    public class ByteExtensionTests
    {
        [TestMethod]
        public void ParseNetstring()
        {
            var ns = "10:0123456789,things";
            var nsBytes = Encoding.ASCII.GetBytes(ns);
            var parsed = nsBytes.ParseNetstring().Select(b => Encoding.ASCII.GetString(b)).ToArray();
            Assert.AreEqual("0123456789", parsed[0]);
            Assert.AreEqual("things", parsed[1]);
        }

        [TestMethod]
        public void Split()
        {
            testSplit("turtles are p cool just saying", 4); //single seperators
            testSplit(" turtles  are  p  cool just saying", 4); //multiple seperators
            testSplit("turtles are p   cool just saying", 4); //spaces at the begin of the extra bytes
        }

        private void testSplit(string str, int count)
        {
            var stringSplit = str.Split(new char[] { ' ' }, count);
            var byteSplit = Encoding.ASCII.GetBytes(str).Split(' ', count).Select(b => Encoding.ASCII.GetString(b)).ToList();

            Assert.AreEqual(stringSplit.Length, byteSplit.Count, "splits did not produce lists of the same length");

            for (int i = 0; i < stringSplit.Length; i++)
            {
                Assert.AreEqual(stringSplit[i], byteSplit[i], "substrings should be the same");
            }
        }
    }
}
