using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace m2net
{
    public enum TnetStringType
    {
        Int,
        Dict,
        List,
        Bool,
        Null,
        String
    }

    public class TnetString
    {
        public TnetString(int data)
        {
            this.IntValue = data;
            this.Type = TnetStringType.Int;
        }
        public TnetString(Dictionary<string, TnetString> data)
        {
            this.DictValue = data;
            this.Type = TnetStringType.Dict;
        }
        public TnetString(List<TnetString> data)
        {
            this.ListValue = data;
            this.Type = TnetStringType.List;
        }
        public TnetString(bool data)
        {
            this.BoolValue = data;
            this.Type = TnetStringType.Bool;
        }
        public TnetString(ArraySegment<byte> data)
        {
            this.StringValue = data;
            this.Type = TnetStringType.String;
        }
        public TnetString()
        {
            this.Type = TnetStringType.Null;
        }

        public TnetStringType Type;
        public int IntValue;
        public Dictionary<string, TnetString> DictValue;
        public List<TnetString> ListValue;
        public bool BoolValue = false;
        public ArraySegment<byte> StringValue;
    }

    public static class Tnetstring
    {
        private struct TnetstringPayload
        {
            public TnetstringPayload(ArraySegment<byte> payload, byte payloadType, ArraySegment<byte> remain)
            {
                this.Payload = payload;
                this.PayloadType = payloadType;
                this.Remain = remain;
            }

            public ArraySegment<byte> Payload;
            public byte PayloadType;
            public ArraySegment<byte> Remain;
        }

        public struct TParseResult
        {
            public TParseResult(TnetString data, ArraySegment<byte> remain)
            {
                this.Data = data;
                this.Remain = remain;
            }

            public TnetString Data;
            public ArraySegment<byte> Remain;
        }

        public static TParseResult TParse(this ArraySegment<byte> data)
        {
            var parsed = TParsePayload(data);
            var payload = parsed.Payload;
            var payloadType = (char)parsed.PayloadType;
            var remain = parsed.Remain;

            switch (payloadType)
            {
                case '#':
                    return new TParseResult(new TnetString(int.Parse(payload.ToAsciiString())), remain);
                case '}':
                    return new TParseResult(ParseDict(payload), remain);
                case ']':
                    return new TParseResult(ParseList(payload), remain);
                case '!':
                    return new TParseResult(new TnetString(payload.ToAsciiString() == "true"), remain);
                case '~':
                    if (payload.Count != 0)
                        throw new Exception("Payload must be 0 length for null.");
                    return new TParseResult(new TnetString(), remain);
                case ',':
                    return new TParseResult(new TnetString(payload), remain);
                default:
                    throw new Exception("Invalid payload type: " + payloadType);
            }

            throw new NotImplementedException();
        }

        static TnetstringPayload TParsePayload(this ArraySegment<byte> data)
        {
            //assert data, "Invalid data to parse, it's empty."
            if (data == null)
                throw new ArgumentNullException("data");

            //length, extra = data.split(':', 1)
            //length = int(length)
            var dataSplit = data.Split(':', 2);
            var length = int.Parse(dataSplit[0].ToAsciiString());
            var extra = dataSplit[1];

            //payload, extra = extra[:length], extra[length:]
            var payload = extra.Substring(0, length);
            extra = extra.Substring(length);

            //assert extra, "No payload type: %r, %r" % (payload, extra)
            if (extra.Count == 0)
                throw new Exception("No payload type");

            //payload_type, remain = extra[0], extra[1:]
            var payloadType = extra.Get(0);
            var remain = extra.Substring(1);

            //assert len(payload) == length, "Data is wrong length %d vs %d" % (length, len(payload))
            if (payload.Count != length)
                throw new Exception("Data is wrong length");

            //return payload, payload_type, remain
            return new TnetstringPayload(payload, payloadType, remain);
        }

        static TnetString ParseList(ArraySegment<byte> data)
        {
            var ret = new List<TnetString>();

            while (data.Count != 0)
            {
                var parsed = data.TParse();
                ret.Add(parsed.Data);
                data = parsed.Remain;
            }

            return new TnetString(ret);
        }

        private struct TPair
        {
            public string Key;
            public TnetString Value;
            public ArraySegment<byte> Extra;
        }

        static TPair ParsePair(ArraySegment<byte> data)
        {
            var parsed = data.TParse();

            if (parsed.Data.Type != TnetStringType.String)
                throw new Exception("Dictionary key must be a string.");

            var key = parsed.Data.StringValue.ToAsciiString();
            var extra = parsed.Remain;

            if (extra.Count == 0)
                throw new Exception("Unbalanced dictionary store.");

            parsed = extra.TParse();
            extra = parsed.Remain;

            return new TPair() { Key = key, Value = parsed.Data, Extra = extra };
        }

        static TnetString ParseDict(ArraySegment<byte> data)
        {
            var ret = new Dictionary<string, TnetString>();

            while (data.Count != 0)
            {
                var pair = ParsePair(data);
                ret[pair.Key] = pair.Value;
                data = pair.Extra;
            }

            return new TnetString(ret);
        }
    }
}
