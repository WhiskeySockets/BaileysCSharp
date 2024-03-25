using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Sockets
{
    public abstract class BusinessSocket : MessagesRecvSocket
    {
        public BusinessSocket([NotNull] SocketConfig config) : base(config)
        {
        }
    }
}
