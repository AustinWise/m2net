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

        public void Host(OwinApplication app)
        {
            while (true)
            {
                var req = conn.Receive();
                var owinReq = convertRequestToOwin(req);
                app(owinReq, (code, headers, body) =>
                {
                    var splitCode = code.Split(new[] { ' ' }, 1);
                    var intCode = int.Parse(splitCode[0]);

                    bool sentHeads = false;

                    body((bodyBytes, _) =>
                    {
                        if (sentHeads)
                            conn.Reply(req, bodyBytes);
                        else
                        {
                            conn.ReplyHttp(req, bodyBytes, intCode, splitCode[1], headers);
                            sentHeads = true;
                        }
                        return false;
                    }, null, null);
                }, _ => { });
            }
        }
    }
}
