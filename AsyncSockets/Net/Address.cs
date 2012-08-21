using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AddressFamily = System.Net.Sockets.AddressFamily;

namespace AsyncSockets.Net
{
    public class Address
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public string Family { get; set; }

        public override string ToString()
        {
            if (Family == AddressFamily.InterNetworkV6.ToString())
                return string.Format("[{0}]:{1}", IP, Port);
            else
                return string.Format("{0}:{1}", IP, Port);
        }
    }
}
