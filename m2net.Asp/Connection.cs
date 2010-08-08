/* **********************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This source code is subject to terms and conditions of the Microsoft Public
 * License (Ms-PL). A copy of the license can be found in the license.htm file
 * included in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * **********************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Linq;

namespace Cassini
{
    class Connection : MarshalByRefObject
    {
        Server _server;
        m2net.Request _mongrel2Request;
        private List<byte[]> _thingsToSend = new List<byte[]>();
        private string _bodyAsAscii = null;

        internal Connection(Server server, m2net.Request mongrel2Request)
        {
            _server = server;
            _mongrel2Request = mongrel2Request;
        }

        public override object InitializeLifetimeService()
        {
            // never expire the license
            return null;
        }

        public bool Connected { get { return _mongrel2Request != null; } }

        public void Close()
        {
            int totalLength = (from d in _thingsToSend select d.Length).Aggregate((a, b) => a + b);
            byte[] data = new byte[totalLength];
            int bytesCopied = 0;
            foreach (var bytes in _thingsToSend)
            {
                Buffer.BlockCopy(bytes, 0, data, bytesCopied, bytes.Length);
                bytesCopied += bytes.Length;
            }

            _server.Mongrel2Connection.Reply(_mongrel2Request, data);
            _mongrel2Request = null;
        }

        static string MakeResponseHeaders(int statusCode, string moreHeaders, int contentLength, bool keepAlive)
        {
            var sb = new StringBuilder();

            sb.Append("HTTP/1.1 " + statusCode + " " + HttpWorkerRequest.GetStatusDescription(statusCode) + "\r\n");
            sb.Append("Server: Cassini/" + Messages.VersionString + "\r\n");
            sb.Append("Date: " + DateTime.Now.ToUniversalTime().ToString("R", DateTimeFormatInfo.InvariantInfo) + "\r\n");
            if (contentLength >= 0)
                sb.Append("Content-Length: " + contentLength + "\r\n");
            if (moreHeaders != null)
                sb.Append(moreHeaders);
            if (!keepAlive)
                sb.Append("Connection: Close\r\n");
            sb.Append("\r\n");

            return sb.ToString();
        }

        static String MakeContentTypeHeader(string fileName)
        {
            Debug.Assert(File.Exists(fileName));
            string contentType = null;

            var info = new FileInfo(fileName);
            string extension = info.Extension.ToLowerInvariant();

            switch (extension)
            {
                case ".bmp":
                    contentType = "image/bmp";
                    break;

                case ".css":
                    contentType = "text/css";
                    break;

                case ".gif":
                    contentType = "image/gif";
                    break;

                case ".ico":
                    contentType = "image/x-icon";
                    break;

                case ".htm":
                case ".html":
                    contentType = "text/html";
                    break;

                case ".jpe":
                case ".jpeg":
                case ".jpg":
                    contentType = "image/jpeg";
                    break;

                case ".js":
                    contentType = "application/x-javascript";
                    break;

                default:
                    break;
            }

            if (contentType == null)
            {
                return null;
            }

            return "Content-Type: " + contentType + "\r\n";
        }

        string GetErrorResponseBody(int statusCode, string message)
        {
            string body = Messages.FormatErrorMessageBody(statusCode, _server.VirtualPath);
            if (message != null && message.Length > 0)
            {
                body += "\r\n<!--\r\n" + message + "\r\n-->";
            }
            return body;
        }

        public Dictionary<string, string> GetHeaders()
        {
            return _mongrel2Request.Headers;
        }

        public byte[] GetBody()
        {
            return _mongrel2Request.Body;
        }

        public int GetBodySize()
        {
            return _mongrel2Request.Body.Length;
        }

        public string ReadRequestBody()
        {
            if (_bodyAsAscii == null)
                _bodyAsAscii = Encoding.ASCII.GetString(_mongrel2Request.Body);
            return _bodyAsAscii;
        }

        public void Write100Continue()
        {
            WriteEntireResponseFromString(100, null, null, true);
        }

        public void WriteBody(byte[] data, int offset, int length)
        {
            byte[] copy = new byte[length];
            Buffer.BlockCopy(data, offset, copy, 0, length);
            this._thingsToSend.Add(copy);
        }

        public void WriteEntireResponseFromString(int statusCode, String extraHeaders, String body, bool keepAlive)
        {
            try
            {
                int bodyLength = (body != null) ? Encoding.UTF8.GetByteCount(body) : 0;
                string headers = MakeResponseHeaders(statusCode, extraHeaders, bodyLength, keepAlive);

                _thingsToSend.Add(Encoding.UTF8.GetBytes(headers + body));
            }
            catch (SocketException)
            {
            }
            finally
            {
                if (!keepAlive)
                {
                    Close();
                }
            }
        }

        public void WriteEntireResponseFromFile(String fileName, bool keepAlive)
        {
            if (!File.Exists(fileName))
            {
                WriteErrorAndClose(404);
                return;
            }

            // Deny the request if the contentType cannot be recognized.
            string contentTypeHeader = MakeContentTypeHeader(fileName);
            if (contentTypeHeader == null)
            {
                WriteErrorAndClose(403);
                return;
            }

            bool completed = false;
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                int len = (int)fs.Length;
                byte[] fileBytes = new byte[len];
                int bytesRead = fs.Read(fileBytes, 0, len);

                String headers = MakeResponseHeaders(200, contentTypeHeader, bytesRead, keepAlive);
                byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

                byte[] data = new byte[headerBytes.Length + len];
                Array.Copy(headerBytes, data, headerBytes.Length);
                Array.Copy(fileBytes, 0, data, headerBytes.Length, len);

                _thingsToSend.Add(data);

                completed = true;
            }
            catch (SocketException)
            {
            }
            finally
            {
                if (!keepAlive || !completed)
                    Close();

                if (fs != null)
                    fs.Close();
            }
        }

        public void WriteErrorAndClose(int statusCode, string message)
        {
            WriteEntireResponseFromString(statusCode, null, GetErrorResponseBody(statusCode, message), false);
        }

        public void WriteErrorAndClose(int statusCode)
        {
            WriteErrorAndClose(statusCode, null);
        }

        public void WriteHeaders(int statusCode, String extraHeaders)
        {
            string headers = MakeResponseHeaders(statusCode, extraHeaders, -1, false);

            _thingsToSend.Add(Encoding.ASCII.GetBytes(headers));
        }
    }
}
