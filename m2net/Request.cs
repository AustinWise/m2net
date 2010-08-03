using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;

namespace m2net
{
    public class Request
    {
        public string Sender { get; private set; }
        public string ConnId { get; private set; }
        public string Path { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string Body { get; private set; }
        public JsonBuffer Data { get; private set; }

        public bool IsDisconnect
        {
            get
            {
                return this.Headers["METHOD"] == "JSON" && this.Data.GetMembers().Where(m => m.Name == "type").First().Buffer.GetString() == "disconnect";
            }
        }

        internal Request(string sender, string conn_id, string path, string headers, string body)
        {
            this.Sender = sender;
            this.ConnId = conn_id;
            this.Path = path;
            this.Headers = new Dictionary<string, string>();
            foreach (var h in JsonBuffer.From(headers).GetMembers())
            {
                this.Headers.Add(h.Name, h.Buffer.GetString());
            }
            this.Body = body;

            if (this.Headers["METHOD"] == "JSON")
                this.Data = JsonBuffer.From(body);
            else
                this.Data = new JsonBuffer();
        }

        public static Request Parse(string msg)
        {
            var threeChunks = msg.Split(new char[] { ' ' }, 4);
            var sender = threeChunks[0];
            var conn_id = threeChunks[1];
            var path = threeChunks[2];
            var rest = threeChunks[3];

            var headersAndRest = parse_netstring(rest);
            var headers = headersAndRest[0];
            rest = headersAndRest[1];
            
            var body = parse_netstring(rest)[0];

            return new Request(sender, conn_id, path, headers, body);
        }

        private static string[] parse_netstring(string ns)
        {
            string[] lenAndRest = ns.Split(new char[] { ':' }, 2);
            string rest = lenAndRest[1];
            int len = int.Parse(lenAndRest[0]);
            if (rest[len] != ',')
                throw new FormatException("Netstring did not end in ','");
            return new string[] { rest.Substring(0, len), rest.Substring(len + 1) };
        }
    }
}
