using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Sockets
{
    public abstract class MessagesSocket : GroupSocket
    {
        public MessagesSocket([NotNull] SocketConfig config) : base(config)
        {
        }
    }
}
