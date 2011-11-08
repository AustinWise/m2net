using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace m2net
{
    /// <summary>
    /// Provides an OWIN interface to Mongrel2.
    /// </summary>
    /// <remarks>
    /// See http://owin.org/ for information about OWIN.
    /// </remarks>
    public class OwinServer
    {
        public delegate Action BodyDelegate(Func<
                                                ArraySegment<byte>, // data
                                                Action, // continuation
                                                bool // will invoke continuation
                                                > onNext,
                                            Action<Exception> onError,
                                            Action onComplete);

        public delegate void OwinApplication(IDictionary<string, object> env,
                                             Action<string, IDictionary<string, string>, BodyDelegate> responseCallback,
                                             Action<Exception> onError);


        private Connection conn;
        private static readonly Action emptyAction = new Action(() => { });

        public OwinServer(Connection conn)
        {
            if (conn == null)
                throw new ArgumentNullException();
            this.conn = conn;

            throw new NotImplementedException("This class is not done.");
        }

        private IDictionary<string, object> convertRequestToOwin(Request req)
        {
            var dic = new Dictionary<string, object>();

            var uri = new Uri(req.Headers["URI"]);

            dic["owin.RequestMethod"] = req.Headers["METHOD"];
            dic["owin.RequestPath"] = req.Path;
            dic["owin.RequestPathBase"] = req.Path;
            dic["owin.RequestQueryString"] = req.Headers["QUERY"];
            dic["owin.RequestHeaders"] = req.Headers;
            dic["owin.RequestBody"] = new BodyDelegate((onNext, onError, onComplete) =>
            {
                onNext(new ArraySegment<byte>(req.Body), null);
                onComplete();
                return emptyAction;
            });
            dic["owin.RequestScheme"] = "http";
            dic["owin.Version"] = "1.0";

            return dic;
        }

        private class InFlightResponse
        {
            private Request mRequest;
            private IDictionary<string, string> mHeaders;
            private Connection mConnection;
            private int mResponseCode;
            private string mReponseMessage;

            public InFlightResponse(Connection connection, Request request, IDictionary<string, string> headers,
                int resCode, string resMessage)
            {
                this.mConnection = connection;
                this.mRequest = request;
                this.mHeaders = headers;
                this.mResponseCode = resCode;
                this.mReponseMessage = resMessage;
            }

            public bool OnNext(ArraySegment<byte> bodyBytes, Action continuation)
            {
                lock (this)
                {
                    if (mHeaders == null)
                        mConnection.Reply(mRequest, bodyBytes);
                    else
                    {
                        mConnection.ReplyHttp(mRequest, bodyBytes, mResponseCode, mReponseMessage, mHeaders);
                        mHeaders = null;
                    }
                    return false;
                }
            }

            public void OnError(Exception ex)
            {
            }

            public void OnComplete()
            {
            }
        }

        private void SendResponse(Request req, string code, IDictionary<string, string> headers, BodyDelegate body)
        {
            var splitCode = code.Split(new[] { ' ' }, 1);
            var intCode = int.Parse(splitCode[0]);

            var res = new InFlightResponse(conn, req, headers, intCode, splitCode[1]);
            body(res.OnNext, res.OnError, res.OnComplete);
        }

        public void Host(OwinApplication app)
        {
            while (true)
            {
                var req = conn.Receive();
                var owinReq = convertRequestToOwin(req);
                app(owinReq, (code, headers, body) => SendResponse(req, code, headers, body), _ => { });
            }
        }
    }
}
