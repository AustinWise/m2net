using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace m2net
{
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

        public struct TParseResult<T>
        {
            public TParseResult(T data, ArraySegment<byte> remain)
            {
                this.Data = data;
                this.Remain = remain;
            }

            public T Data;
            public ArraySegment<byte> Remain;
        }

        static TParseResult<T> TParse<T>(this ArraySegment<byte> data)
        {
            var parsed = TParse(data);

            //TODO: handel ~ null
            return new TParseResult<T>((T)parsed.Data, parsed.Remain);
        }

        public static TParseResult<object> TParse(this ArraySegment<byte> data)
        {
            var parsed = TParsePayload(data);
            var payload = parsed.Payload;
            var payloadType = (char)parsed.PayloadType;
            var remain = parsed.Remain;

            switch (payloadType)
            {
                case '#':
                    return new TParseResult<object>(int.Parse(payload.ToAsciiString()), remain);
                case '}':
                    return new TParseResult<object>(ParseDict(payload), remain);
                case ']':
                    return new TParseResult<object>(ParseList(payload), remain);
                case '!':
                    return new TParseResult<object>(payload.ToAsciiString() == "true", remain);
                case '~':
                    if (payload.Count != 0)
                        throw new Exception("Payload must be 0 length for null.");
                    return new TParseResult<object>(null, remain);
                case ',':
                    return new TParseResult<object>(payload, remain);
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

        static IList<object> ParseList(ArraySegment<byte> data)
        {
            var ret = new List<object>();

            while (data.Count != 0)
            {
                var parsed = data.TParse();
                ret.Add(parsed.Data);
                data = parsed.Remain;
            }

            return ret;
        }

        private struct TPair
        {
            public string Key;
            public object Value;
            public ArraySegment<byte> Extra;
        }

        static TPair ParsePair(ArraySegment<byte> data)
        {
            var parsed = data.TParse();

            if (!(parsed.Data is ArraySegment<byte>))
                throw new Exception("Dictionary key must be a string.");

            var key = ((ArraySegment<byte>)parsed.Data).ToAsciiString();
            var extra = parsed.Remain;

            if (extra.Count == 0)
                throw new Exception("Unbalanced dictionary store.");

            parsed = extra.TParse();
            var val = parsed.Data;
            extra = parsed.Remain;

            if (val == null)
                throw new Exception("Got an invalid value, null not allowed.");

            return new TPair() { Key = key, Value = val, Extra = extra };
        }

        static Dictionary<string, object> ParseDict(ArraySegment<byte> data)
        {
            var ret = new Dictionary<string, object>();

            while (data.Count != 0)
            {
                var pair = ParsePair(data);
                ret[pair.Key] = pair.Value;
                data = pair.Extra;
            }

            return ret;
        }
    }
}
