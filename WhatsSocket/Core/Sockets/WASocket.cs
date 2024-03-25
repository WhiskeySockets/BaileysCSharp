using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Sockets
{
    public class WASocket : BusinessSocket
    {
        public WASocket([NotNull] SocketConfig config) : base(config)
        {
        }
    }
}
