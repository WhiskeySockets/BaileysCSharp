using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Sockets.Client;

namespace WhatsSocket.Core.Events
{
    public delegate void ConnectEventArgs(AbstractSocketClient sender);




    public enum DisconnectReason
    {
        ConnectionClosed = 428,
        ConnectionLost = 408,
        ConnectionReplaced = 440,
        TimedOut = 408,
        LoggedOut = 401,
        BadSession = 500,
        RestartRequired = 515,
        MultideviceMismatch = 411,
        MissMatch = 901
    }

    public delegate void DisconnectEventArgs(AbstractSocketClient sender, DisconnectReason reason);



    public class DataFrame
    {
        public byte[] Buffer { get; set; }
    }
    public delegate void MessageArgs(AbstractSocketClient sender, DataFrame frame);
}
