using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using m2net;

namespace m2net_Tests
{
    [TestClass]
    public class TNetStringTest
    {
        [TestMethod]
        public void TestParse()
        {
            foreach (var line in Properties.Resources.request_payloads.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                var bytes = Encoding.ASCII.GetBytes(line);
                var threeChunks = bytes.Split(' ', 4);
                var parseRes = threeChunks[3].TParse();
            }
        }
    }
}
