using System;
using System.Collections.Generic;
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
        public byte[] Body { get; private set; }
        public JsonBuffer Data { get; private set; }

        public bool IsDisconnect
        {
            get
            {
                if (this.Headers["METHOD"] != "JSON")
                    return false;
                foreach (var m in Data.GetMembers())
                {
                    if (m.Name == "type" && m.Buffer.GetString() == "disconnect")
                        return true;
                }
                return false;
            }
        }

        internal Request(string sender, string conn_id, string path, string headers, byte[] body)
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
                this.Data = JsonBuffer.From(body.ToAsciiString());
            else
                this.Data = new JsonBuffer();
        }

        public static Request Parse(byte[] msg)
        {
            var threeChunks = msg.Split(' ', 4);
            var sender = threeChunks[0].ToAsciiString();
            var conn_id = threeChunks[1].ToAsciiString();
            var path = threeChunks[2].ToAsciiString();
            var rest = threeChunks[3];

            var headersAndRest = rest.ParseNetstring();
            var headers = headersAndRest[0].ToAsciiString();
            rest = headersAndRest[1];

            var body = rest.ParseNetstring()[0];

            return new Request(sender, conn_id, path, headers, body.ToArray());
        }
    }
}
