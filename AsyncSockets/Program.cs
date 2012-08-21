using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AsyncSockets.Core;
using AsyncSockets.Net;

namespace AsyncSockets
{
    class Program
    {
        static void Main(string[] args)
        {

            var server = new Net.Server(conn =>
            {
                Console.WriteLine("Connection accepted from {0}",
                    conn.RemoteAddress.ToString());

                conn.OnClose += wasError =>
                {
                    Console.WriteLine("Connection from {0} was closed",
                        conn.RemoteAddress.ToString());
                };

                conn.OnData += data =>
                {
                    byte[] response = Encoding.ASCII.GetBytes(
                        "HTTP/1.0 200 OK\r\n" +
                        "Content-Type:text/plain\r\n" +
                        "Content-Length:" + data.Length + "\r\n" +
                        "\r\n");

                    int len = response.Length;

                    Array.Resize(ref response,
                        response.Length + data.Length);

                    //Array.Copy(data, 0, response, len, data.Length);

                    for (int i = 0; i < data.Length; i++)
                        response[i + len] = data[i];

                    conn.Write(response, () =>
                    {
                        conn.End();
                    });
                };
            }).Listen(8080, listeningListener: () =>
            {
                Console.WriteLine("Listening on port 8080");
            });

            EventLoop.Run();
        }
    }
}
