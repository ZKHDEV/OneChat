using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace OneChat.Model
{
    public class ClientInfo
    {
        public string Name { set; get; }
        public StreamSocket Socket { set; get; }
    }
}
