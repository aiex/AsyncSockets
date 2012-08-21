using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Wininet = System.Net;
using Winsock = System.Net.Sockets;

namespace AsyncSockets.Net
{
    public class Server
    {
        private Winsock.Socket nativeSocket;
        private Wininet.IPEndPoint endPoint;

        public event Action OnListening;
        public event Action<Socket> OnConnection;
        public event Action OnClose;
        public event Action<Exception> OnError;

        public Address Address
        {
            get
            {
                return new Address
                {
                    IP = endPoint.Address.ToString(),
                    Port = endPoint.Port,
                    Family = endPoint.AddressFamily.ToString()
                };
            }
        }
        public int MaxConnections { get; set; }
        public int Connections
        {
            get { throw new NotImplementedException(); }
        }

        public Server(Action<Socket> connectionHandler)
        {
            if (connectionHandler == null)
                throw new ArgumentNullException("connectionHandler");

            OnConnection += connectionHandler;
        }

        public Server Listen(int port, string host = null,
            int backLog = 128, Action listeningListener = null)
        {
            host = host ?? "0.0.0.0";

            if (listeningListener != null)
                OnListening += listeningListener;

            Core.EventLoop.Instance.Push(() =>
            {
                endPoint = new Wininet.IPEndPoint(
                    Wininet.IPAddress.IPv6Any,
                    port);

                Core.EventLoop.Instance.Push(() =>
                {
                    nativeSocket = new Winsock.Socket(
                        Winsock.AddressFamily.InterNetworkV6,
                        Winsock.SocketType.Stream,
                        Winsock.ProtocolType.Tcp);
                });
                Core.EventLoop.Instance.Push(() =>
                {
                    nativeSocket.SetSocketOption(
                        Winsock.SocketOptionLevel.IPv6,
                        Winsock.SocketOptionName.IPv6Only,
                        false);
                });
                Core.EventLoop.Instance.Push(() =>
                {
                    nativeSocket.Bind(endPoint);
                    nativeSocket.Listen(128);
                });

                if (OnListening != null)
                    Core.EventLoop.Instance.Push(() =>
                    {
                        OnListening();
                    });

                _acceptConnection();
            });

            return this;
        }

        public void Close(Action closeListener = null)
        {
            if (closeListener == null)
                throw new ArgumentNullException("closeListener");

            throw new NotImplementedException();
        }

        private void _acceptConnection()
        {
            Core.EventLoop.Instance.Push(() =>
            {
                nativeSocket.BeginAccept(_static_endAccept, this);
            });
        }

        private static void _static_endAccept(IAsyncResult res)
        {
            var instance = res.AsyncState as Server;
            instance._instance_endAccept(res);
        }

        private void _instance_endAccept(IAsyncResult res)
        {
            Core.EventLoop.Instance.Push(() =>
            {
                var rawSocket = nativeSocket.EndAccept(res);

                var socket = new Socket(rawSocket);

                if (OnConnection != null)
                    Core.EventLoop.Instance.Push(() =>
                    {
                        OnConnection(socket);
                    });

                Core.EventLoop.Instance.Push(() =>
                {
                    _acceptConnection();
                });
            });
        }
    }
}
