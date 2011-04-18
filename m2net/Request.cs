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

        internal Request(string sender, string conn_id, string path, Dictionary<string, string> headers, byte[] body)
        {
            this.Sender = sender;
            this.ConnId = conn_id;
            this.Path = path;
            this.Headers = headers;
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

            var headersAndRest = rest.TParse();
            var headers = headersAndRest.Data;
            rest = headersAndRest.Remain;

            var body = rest.ParseNetstring()[0];

            Dictionary<string, string> headersDic;
            if (headers.Type == TnetStringType.String)
                headersDic = ParseJsonHeaders(headers.StringValue);
            else if (headers.Type == TnetStringType.Dict)
                headersDic = ParseTnsHeaders(headers.DictValue);
            else
                throw new Exception("Unknown header payload.");

            return new Request(sender, conn_id, path, headersDic, body.ToArray());
        }

        private static Dictionary<string, string> ParseJsonHeaders(ArraySegment<byte> data)
        {
            var headers = new Dictionary<string, string>();
            foreach (var h in JsonBuffer.From(data.ToAsciiString()).GetMembers())
            {
                var key = h.Name;
                if (h.Buffer.IsScalar)
                    headers.Add(key, h.Buffer.GetString());
                else if (h.Buffer.IsArray)
                {
                    for (int i = 1; i < h.Buffer.GetArrayLength(); i++)
                    {
                        var v = h.Buffer[i];
                        if (v.Class != JsonTokenClass.String)
                            throw new Exception("non-string value header");
                        if (headers.ContainsKey(key))
                            break; //TODO: support many header values
                        headers.Add(key, v.Text);
                    }
                }
                else
                {
                    throw new Exception("Unexpected JSON type.");
                }
            }
            return headers;
        }

        private static Dictionary<string, string> ParseTnsHeaders(Dictionary<string, TnetString> objDic)
        {
            var ret = new Dictionary<string, string>();

            foreach (var kvp in objDic)
            {
                var key = kvp.Key;
                if (kvp.Value.Type == TnetStringType.String)
                {
                    ret.Add(key, kvp.Value.StringValue.ToAsciiString());
                }
                else if (kvp.Value.Type == TnetStringType.List)
                {
                    foreach (TnetString value in kvp.Value.ListValue)
                    {
                        if (ret.ContainsKey(key))
                            break; //TODO: many headers
                        if (value.Type != TnetStringType.String)
                            throw new Exception("Expected string values.");
                        ret.Add(key, value.StringValue.ToAsciiString());
                    }
                }
                else
                {
                    throw new Exception("Unexpected header type.");
                }
            }

            return ret;
        }
    }
}
