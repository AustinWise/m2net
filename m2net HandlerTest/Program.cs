using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using m2net;

namespace m2net.HandlerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string vboxIp = "10.5.2.202";
            var conn = new Connection("54c6755b-9628-40a4-9a2d-cc82a816345e", "tcp://" + vboxIp + ":9997", "tcp://" + vboxIp + ":9996");
            HandlerTest(conn);
        }

        static void HandlerTest(Connection conn)
        {
            while (true)
            {
                Console.WriteLine("WAITING FOR REQUEST");
                var r = conn.Receive();

                if (r.IsDisconnect)
                {
                    Console.WriteLine("DISCONNECT");
                    continue;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Sender: {0} Ident: {1} Path: {2} Headers:", r.Sender, r.ConnId, r.Path);
                sb.AppendLine();
                foreach (var h in r.Headers)
                {
                    sb.AppendFormat("\t{0}: {1}", h.Key, h.Value);
                    sb.AppendLine();
                }

                conn.ReplyHttp(r, sb.ToString(), 200, "OK", new Dictionary<string, string>() { { "Content-Type", "text/plain" } });
            }
        }

    }
}
