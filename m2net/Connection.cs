using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace m2net
{
    public class Connection : MarshalByRefObject, IDisposable
    {
        private ZMQ.Context CTX;
        public const int IoThreads = 1;
        private Encoding Enc = Encoding.ASCII;

        private string sub_addr;
        private string pub_addr;
        public string SenderId { get; private set; }
        private bool isRunning = true;

        private AutoResetEvent itemsReadyToSend = new AutoResetEvent(false);
        private Thread sendThread;
        private Queue<byte[]> sendQ = new Queue<byte[]>();

        private AutoResetEvent itemsReadyToRecv = new AutoResetEvent(false);
        private Thread recvThread;
        private Queue<byte[]> recvQ = new Queue<byte[]>();

        private int threadStillRunning = 2;

        public Connection(string sender_id, string sub_addr, string pub_addr)
        {
            CTX = new ZMQ.Context(IoThreads);

            this.SenderId = sender_id;

            this.sub_addr = sub_addr;
            this.pub_addr = pub_addr;

            sendThread = new Thread(sendProc);
            sendThread.Name = "Mongrel2 Connection send thread";
            sendThread.IsBackground = true;
            sendThread.Start();

            recvThread = new Thread(recvProc);
            recvThread.Name = "Mongrel2 Connection receive thread";
            recvThread.IsBackground = true;
            recvThread.Start();
        }

        private void sendProc()
        {
            ZMQ.Socket resp;

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
                        byte[] stuffToSend = sendQ.Dequeue();
                        while (!resp.Send(stuffToSend))
                            ;
                    }
                }
            }

            resp.Dispose();
            itemsReadyToSend.Close();
            Interlocked.Decrement(ref threadStillRunning);
        }

        private void recvProc()
        {
            ZMQ.Socket reqs;

            if (!isRunning)
                throw new ObjectDisposedException("Connection");

            reqs = CTX.Socket(ZMQ.PULL);
            reqs.Connect(sub_addr);

            while (isRunning)
            {
                byte[] data;
                bool gotIt = reqs.Recv(out data, ZMQ.NOBLOCK);

                if (gotIt)
                {
                    lock (recvQ)
                    {
                        recvQ.Enqueue(data);
                    }
                    itemsReadyToRecv.Set();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

            reqs.Dispose();
            itemsReadyToRecv.Close();
            Interlocked.Decrement(ref threadStillRunning);
        }

        public Request Receive()
        {

            byte[] data = null;

            while (data == null)
            {
                if (!isRunning)
                    throw new ObjectDisposedException("Connection");

                if (recvQ.Count != 0)
                {
                    lock (recvQ)
                    {
                        if (recvQ.Count != 0)
                            data = recvQ.Dequeue();
                    }
                }
                else
                {
                    itemsReadyToRecv.WaitOne(1);
                }
            }

            return Request.Parse(data);
        }

        public void Send(string uuid, string conn_id, byte[] msg)
        {
            Send(uuid, conn_id, msg, 0, msg.Length);
        }

        public void Send(string uuid, string conn_id, byte[] msg, int offset, int length)
        {
            if (!isRunning)
                throw new ObjectDisposedException("Connection");

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
            itemsReadyToSend.Set(); // wake up the send thread

            //wait till the threads have closed their sockets before closing the context
            while (threadStillRunning != 0)
                Thread.Sleep(1);

            CTX.Dispose();
        }
    }
}
