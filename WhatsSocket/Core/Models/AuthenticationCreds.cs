using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsSocket.Core.Models
{
    public enum DisconnectReason
    {
        ConnectionClosed = 428,
        ConnectionLost = 408,
        ConnectionReplaced = 440,
        TimedOut = 408,
        LoggedOut = 401,
        BadSession = 500,
        RestartRequired = 515,
        MultideviceMismatch = 411
    }

}
