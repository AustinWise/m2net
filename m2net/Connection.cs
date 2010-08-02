using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace m2net
{
    public class Connection : IDisposable
    {
        private static ZMQ.Context CTX;
        public const int IoThreads = 1;
        private Encoding Enc = Encoding.ASCII;

        static Connection()
        {
            CTX = new ZMQ.Context(IoThreads);
        }

        private ZMQ.Socket reqs;
        private ZMQ.Socket resp;
        private string sub_addr;
        private string pub_addr;
        public string SenderId { get; private set; }

        public Connection(string sender_id, string sub_addr, string pub_addr)
        {
            this.SenderId = sender_id;

            reqs = CTX.Socket(ZMQ.UPSTREAM);
            reqs.Connect(sub_addr);

            resp = CTX.Socket(ZMQ.PUB);
            resp.Connect(pub_addr);
            resp.SetSockOpt(ZMQ.IDENTITY, sender_id);

            this.sub_addr = sub_addr;
            this.pub_addr = pub_addr;
        }

        public Request Receive()
        {
            byte[] msg;
            while (!this.reqs.Recv(out msg))
                ;

            return Request.Parse(Encoding.ASCII.GetString(msg));
        }

        public void Send(string uuid, string conn_id, byte[] msg)
        {
            Send(uuid, conn_id, msg, 0, msg.Length);
        }

        public void Send(string uuid, string conn_id, byte[] msg, int offset, int length)
        {
            string header = string.Format("{0} {1}:{2}, ", uuid, conn_id.Length, conn_id);
            byte[] headerBytes = Enc.GetBytes(header);
            byte[] data = new byte[headerBytes.Length + length];
            Array.Copy(headerBytes, data, headerBytes.Length);
            Array.Copy(msg, offset, data, headerBytes.Length, length);
            while (!this.resp.Send(data))
                ;
        }

        public void Send(string uuid, string conn_id, string msg)
        {
            Send(uuid, conn_id, Enc.GetBytes(msg));
        }


        public void Deliver(string uuid, string[] idents, byte[] data)
        {
            Send(uuid, string.Join(" ", idents), data);
        }

        public void Reply(Request req, byte[] msg, int offset, int length)
        {
            this.Send(req.Sender, req.ConnId, msg, offset, length);
        }

        public void Reply(Request req, byte[] msg)
        {
            this.Send(req.Sender, req.ConnId, msg);
        }

        public void Reply(Request req, string msg)
        {
            this.Send(req.Sender, req.ConnId, msg);
        }

        private const string HTTP_FORMAT = "HTTP/1.1 {0} {1}\r\n{2}\r\n\r\n";
        private byte[] httpResponse(string body, int code, string status, Dictionary<string, string> headers)
        {
            var bodyBytes = Enc.GetBytes(body);
            headers["Content-Length"] = bodyBytes.Length.ToString();
            var header = string.Format(HTTP_FORMAT, code, status, (headers.Select(kvp => kvp.Key + ": " + kvp.Value)).Aggregate((a, b) => a + "\r\n" + b));
            var headerBytes = Enc.GetBytes(header);
            var ret = new byte[bodyBytes.Length + headerBytes.Length];
            Array.Copy(headerBytes, ret, headerBytes.Length);
            Array.Copy(bodyBytes, 0, ret, headerBytes.Length, bodyBytes.Length);
            //headers["Content-Length"]'
            return ret;
        }

        public void ReplyHttp(Request req, string body, int code, string status, Dictionary<string, string> headers)
        {
            var thingToSend = httpResponse(body, code, status, headers);
            this.Reply(req, thingToSend);
        }

        public void Dispose()
        {
            reqs.Dispose();
            resp.Dispose();
        }
    }
}
