using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Sockets.Client;

namespace WhatsSocket.Core.Events
{

    public delegate void ConnectEventArgs(AbstractSocketClient sender);
    public delegate void DisconnectEventArgs(AbstractSocketClient sender, DisconnectReason reason);



    public class DataFrame
    {
        public byte[] Buffer { get; set; }
    }
    public delegate void MessageArgs(AbstractSocketClient sender, DataFrame frame);
}
