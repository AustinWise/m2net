using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using m2net;

namespace m2net_Tests
{
    [TestClass]
    public class RequestTests
    {
        [TestMethod]
        public void Parse()
        {
            var req = Request.Parse(toBytes(Properties.Resources.SampleRequestWithNoBody));
            Assert.AreEqual(0, req.Body.Length);
            Assert.AreEqual(14, req.Headers.Count);
            Assert.AreEqual("54c6755b-9628-40a4-9a2d-cc82a816345e", req.Sender);
            Assert.AreEqual("19", req.ConnId);
            Assert.AreEqual("/handlertest", req.Path);
        }

        private byte[] toBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }
    }
}
