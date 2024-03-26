using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Sockets
{
    public abstract class MessagesSendSocket : GroupSocket
    {
        public MessagesSendSocket([NotNull] SocketConfig config) : base(config)
        {
        }
    }
}
