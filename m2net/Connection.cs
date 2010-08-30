using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace m2net
{
    public class Connection : MarshalByRefObject, IDisposable
    {
        private ZMQ.Context CTX;
        public const int IoThreads = 20;
        private Encoding Enc = Encoding.ASCII;

        private ZMQ.Socket reqs;
        private ZMQ.Socket resp;
        private string sub_addr;
        private string pub_addr;
        public string SenderId { get; private set; }
        private bool isRunning = true;
        private AutoResetEvent itemsReadyToSend = new AutoResetEvent(false);
        private Thread sendThread;
        private Queue<byte[]> sendQ = new Queue<byte[]>();

        public Connection(string sender_id, string sub_addr, string pub_addr)
        {
            CTX = new ZMQ.Context(IoThreads);

            this.SenderId = sender_id;

            reqs = CTX.Socket(ZMQ.UPSTREAM);
            reqs.Connect(sub_addr);

            this.sub_addr = sub_addr;
            this.pub_addr = pub_addr;

            sendThread = new Thread(sendProc);
            sendThread.Name = "Mongrel2 Connection send thread";
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        private void sendProc()
        {
            resp = CTX.Socket(ZMQ.PUB);
            resp.Connect(pub_addr);
            resp.SetSockOpt(ZMQ.IDENTITY, SenderId);

            while (isRunning)
            {
                itemsReadyToSend.WaitOne();
                lock (sendQ)
                {
                    while (sendQ.Count != 0)
                    {
                        this.resp.Send(sendQ.Dequeue());
                    }
                }
            }
            itemsReadyToSend.Close();
        }

        public Request Receive()
        {
            byte[] msg;
            while (!this.reqs.Recv(out msg))
                ;

            return Request.Parse(msg);
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
            lock (sendQ)
            {
                sendQ.Enqueue(data);
                itemsReadyToSend.Set();
            }
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

        private const string HTTP_FORMAT = "HTTP/1.1 {0} {1}\r\n{2}\r\n";
        private byte[] httpResponse(string body, int code, string status, Dictionary<string, string> headers)
        {
            var bodyBytes = Enc.GetBytes(body);
            headers["Content-Length"] = bodyBytes.Length.ToString();
            var formattedHeaders = new StringBuilder();

            foreach (var kvp in headers)
            {
                formattedHeaders.AppendFormat("{0}: {1}\r\n", kvp.Key, kvp.Value);
            }

            var header = string.Format(HTTP_FORMAT, code, status, formattedHeaders);
            var headerBytes = Enc.GetBytes(header);
            var ret = new byte[bodyBytes.Length + headerBytes.Length];
            Array.Copy(headerBytes, ret, headerBytes.Length);
            Array.Copy(bodyBytes, 0, ret, headerBytes.Length, bodyBytes.Length);
            return ret;
        }

        public void ReplyHttp(Request req, string body, int code, string status, Dictionary<string, string> headers)
        {
            var thingToSend = httpResponse(body, code, status, headers);
            this.Reply(req, thingToSend);
        }

        public void Dispose()
        {
            isRunning = false;
            itemsReadyToSend.Set();
            reqs.Dispose();
            resp.Dispose();
            CTX.Dispose();
        }
    }
}
