using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Wininet = System.Net;
using Winsock = System.Net.Sockets;

namespace AsyncSockets.Net
{
    public class Socket : IDisposable
    {
        private const int CHUNK = ushort.MaxValue;

        private Winsock.Socket nativeSocket;
        private Wininet.IPEndPoint endPoint;
        private Encoding encoding = Encoding.UTF8;

        private Address localEndpoint;
        private Address remoteEndpoint;

        private int _bytesRead = 0;
        private int _bytesWritten = 0;

        private List<byte[]> receiveBuffer;
        private List<byte[]> sendBuffer;

        private byte[] _recv;

        private Action onSentCallback; // Not supported as an event

        public event Action OnConnect;
        public event Action<byte[]> OnData;
        public event Action OnEnd;
        public event Action OnTimeout;
        public event Action OnDrain;
        public event Action<Exception> OnError;
        public event Action<bool> OnClose;

        public int BufferSize
        {
            get { throw new NotImplementedException(); }
        }

        public Address LocalAddress
        {
            get
            {
                return localEndpoint;
            }
        }

        public Address RemoteAddress
        {
            get
            {
                return remoteEndpoint;
            }
        }

        public int BytesRead
        {
            get { return _bytesRead; }
        }

        public int BytesWritten
        {
            get { return _bytesWritten; }
        }

        public Socket()
        {
            receiveBuffer = new List<byte[]>();
            sendBuffer = new List<byte[]>();
        }

        internal Socket(Winsock.Socket rawSocket)
            : this()
        {
            if (rawSocket == null)
                throw new ArgumentNullException("rawSocket");

            if (rawSocket.RemoteEndPoint != null)
            {
                endPoint = rawSocket.RemoteEndPoint as Wininet.IPEndPoint;

                var tempRemoteEndpoint = rawSocket.RemoteEndPoint as Wininet.IPEndPoint;
                remoteEndpoint = new Address
                {
                    IP = tempRemoteEndpoint.Address.ToString(),
                    Port = tempRemoteEndpoint.Port,
                    Family = tempRemoteEndpoint.AddressFamily.ToString()
                };
            }

            var tempLocalEndpoint = rawSocket.LocalEndPoint as Wininet.IPEndPoint;
            localEndpoint = new Address {
                IP = tempLocalEndpoint.Address.ToString(),
                Port = tempLocalEndpoint.Port,
                Family = tempLocalEndpoint.AddressFamily.ToString()
            };

            rawSocket.NoDelay = true;

            nativeSocket = rawSocket;

            Core.EventLoop.Instance.Push(() =>
            {
                _readData();
            });
        }

        public void Connect(int port, string host = null,
            Action connectListener = null)
        {
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException("port");

            host = host ?? "127.0.0.1";

            if (connectListener != null)
                OnConnect += connectListener;

            Core.EventLoop.Instance.Push(() =>
            {
                endPoint = new Wininet.IPEndPoint(
                    Wininet.IPAddress.Parse(host),
                    port);
            });

            Core.EventLoop.Instance.Push(() =>
            {
                nativeSocket.BeginConnect(endPoint,
                    _static_endConnect, this);
            });
        }

        public void SetEncoding(Encoding encoding = null)
        {
            this.encoding = encoding ?? Encoding.UTF8;
        }

        public void Write(byte[] data, Action dataSentCallback = null)
        {
            if (data.Length < 1)
                throw new InvalidOperationException("Cannot send 0 bytes");

            if (dataSentCallback != null)
                this.onSentCallback = dataSentCallback;

            Core.EventLoop.Instance.Push(() =>
            {
                nativeSocket.BeginSend(data, 0, data.Length,
                    Winsock.SocketFlags.None, _static_endSend, this);
            });
        }

        public void Write(string data, Encoding encoding = null,
            Action dataSentCallback = null)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException("data");

            encoding = encoding ?? this.encoding;

            Core.EventLoop.Instance.Push(() =>
            {
                var byteData = encoding.GetBytes(data);

                Core.EventLoop.Instance.Push(() =>
                {
                    Write(byteData, dataSentCallback);
                });
            });
        }

        public void End(byte[] data)
        {
            if (data != null)
            {
                Core.EventLoop.Instance.Push(() =>
                {
                    Write(data, () =>
                    {
                        End(null);
                    });
                });
            }
            else
            {
                Core.EventLoop.Instance.Push(() =>
                {
                    Core.EventLoop.Instance.Push(() =>
                    {
                        nativeSocket.Close();

                        Core.EventLoop.Instance.Push(() =>
                        {
                            if (OnClose != null)
                                OnClose(false);
                        });
                    });
                });
            }
        }

        public void End(string data = null, Encoding encoding = null)
        {
            if (data != null)
            {
                encoding = encoding ?? this.encoding;

                Core.EventLoop.Instance.Push(() =>
                {
                    var byteData = encoding.GetBytes(data);
                    Core.EventLoop.Instance.Push(() =>
                    {
                        End(byteData);
                    });
                });
            }
            else
            {
                Core.EventLoop.Instance.Push(() =>
                {
                    End(null);
                });
            }
        }

        public void Destroy()
        {
            nativeSocket.Close();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void SetTimeout(int timeout, Action timeoutCallback = null)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException("timeout");

            throw new NotImplementedException();

            /*if (timeoutCallback != null)
                OnTimeout += timeoutCallback;*/
        }

        public void SetNoDelay(bool noDelay = true)
        {
            nativeSocket.NoDelay = noDelay;
        }

        public void SetKeepAlive()
        {
            throw new NotImplementedException();
        }

        private void _readData()
        {
            Core.EventLoop.Instance.Push(() =>
            {
                _recv = new byte[CHUNK];

                nativeSocket.BeginReceive(_recv, 0, CHUNK,
                    Winsock.SocketFlags.None, _static_endReceive, this);
            });
        }

        static void _static_endConnect(IAsyncResult res)
        {
            var instance = res.AsyncState as Socket;
            instance._instance_endConnect(res);
        }

        void _instance_endConnect(IAsyncResult res)
        {
            Core.EventLoop.Instance.Push(() =>
            {
                if (OnConnect != null)
                    Core.EventLoop.Instance.Push(() =>
                    {
                        OnConnect();
                    });

                _readData();
            });
        }

        static void _static_endSend(IAsyncResult res)
        {
            var instance = res.AsyncState as Socket;
            instance._instance_endSend(res);
        }

        void _instance_endSend(IAsyncResult res)
        {
            Core.EventLoop.Instance.Push(() =>
            {
                int nSent = nativeSocket.EndSend(res);

                _bytesWritten += nSent;

                if (onSentCallback != null)
                    Core.EventLoop.Instance.Push(() =>
                    {
                        onSentCallback();
                    });
            });
        }

        static void _static_endReceive(IAsyncResult res)
        {
            var instance = res.AsyncState as Socket;
            instance._instance_endReceive(res);
        }

        void _instance_endReceive(IAsyncResult res)
        {
            Core.EventLoop.Instance.Push(() =>
            {
                int nRead = nativeSocket.EndReceive(res);
                _bytesRead += nRead;

                if (OnData != null)
                {
                    Core.EventLoop.Instance.Push(() =>
                    {
                        byte[] data = new byte[nRead];
                        Array.Copy(_recv, data, nRead);

                        Core.EventLoop.Instance.Push(() =>
                        {
                            OnData(data);
                        });
                    });
                }
            });
        }

        public void Dispose()
        {
            Console.WriteLine("===== Disposing native socket");
            nativeSocket.Dispose();
        }
    }
}
