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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Compilation;

namespace Cassini
{
    public class Server : MarshalByRefObject
    {
        string _virtualPath;
        string _physicalPath;
        bool _shutdownInProgress;
        Host _host;
        m2net.Connection _mongrel2Connection;

        /// <summary>
        /// This takes ownership of the mongrel2Connection.  It will be disposed when the server is stopped
        /// and the connection should probably not be reused anyways due to the threading issue with
        /// ZMQ.
        /// </summary>
        /// <param name="mongrel2Connection"></param>
        /// <param name="virtualPath"></param>
        /// <param name="physicalPath"></param>
        public Server(m2net.Connection mongrel2Connection, string virtualPath, string physicalPath)
        {
            _mongrel2Connection = mongrel2Connection;
            _virtualPath = virtualPath;
            _physicalPath = physicalPath.EndsWith("\\", StringComparison.Ordinal) ? physicalPath : physicalPath + "\\";
        }

        public override object InitializeLifetimeService()
        {
            // never expire the license
            return null;
        }

        public m2net.Connection Mongrel2Connection
        {
            get
            {
                return this._mongrel2Connection;
            }
        }

        public string VirtualPath
        {
            get
            {
                return _virtualPath;
            }
        }

        public string PhysicalPath
        {
            get
            {
                return _physicalPath;
            }
        }

        //
        // Socket listening
        // 

        static Socket CreateSocketBindAndListen(AddressFamily family, IPAddress address, int port)
        {
            var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(address, port));
            socket.Listen((int)SocketOptionName.MaxConnections);
            return socket;
        }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                while (!_shutdownInProgress)
                {
                    try
                    {
                        m2net.Request acceptedSocket = _mongrel2Connection.Receive();

                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            if (!_shutdownInProgress)
                            {
                                var conn = new Connection(this, acceptedSocket);

                                //// wait for at least some input
                                //if (conn.WaitForRequestBytes() == 0)
                                //{
                                //    conn.WriteErrorAndClose(400);
                                //    return;
                                //}

                                // find or create host
                                Host host = GetHost();
                                if (host == null)
                                {
                                    conn.WriteErrorAndClose(500);
                                    return;
                                }

                                // process request in worker app domain
                                host.ProcessRequest(conn);
                            }
                        });
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }
            });
        }

        public void Stop()
        {
            _shutdownInProgress = true;

            try
            {
                if (_mongrel2Connection != null)
                {
                    _mongrel2Connection.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                _mongrel2Connection = null;
            }

            try
            {
                if (_host != null)
                {
                    _host.Shutdown();
                }

                while (_host != null)
                {
                    Thread.Sleep(100);
                }
            }
            catch
            {
            }
            finally
            {
                _host = null;
            }
        }

        // called at the end of request processing
        // to disconnect the remoting proxy for Connection object
        // and allow GC to pick it up
        internal void OnRequestEnd(Connection conn)
        {
            RemotingServices.Disconnect(conn);
        }

        public void HostStopped()
        {
            _host = null;
        }

        Host GetHost()
        {
            if (_shutdownInProgress)
                return null;

            Host host = _host;

            if (host == null)
            {
                lock (this)
                {
                    host = _host;
                    if (host == null)
                    {
                        host = (Host)CreateWorkerAppDomainWithHost(_virtualPath, _physicalPath, typeof(Host));
                        host.Configure(this, _virtualPath, _physicalPath);
                        _host = host;
                    }
                }
            }

            return host;
        }

        static object CreateWorkerAppDomainWithHost(string virtualPath, string physicalPath, Type hostType)
        {
            var buildManager = new ClientBuildManager(virtualPath, physicalPath);

            return buildManager.CreateObject(hostType, false);            
        }
    }
}
