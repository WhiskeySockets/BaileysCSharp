using System.Diagnostics.CodeAnalysis;
using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Sockets
{

    public class WASocket : NewsletterSocket
    {
        public WASocket([NotNull] SocketConfig config) : base(config)
        {
        }

    }
}
