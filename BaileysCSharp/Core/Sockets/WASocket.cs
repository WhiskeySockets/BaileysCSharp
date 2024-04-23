using System.Diagnostics.CodeAnalysis;
using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Sockets
{
    public class WASocket : BusinessSocket
    {
        public WASocket([NotNull] SocketConfig config) : base(config)
        {
        }

        public List<ContactModel> GetAllGroups()
        {
            return Store.GetAllGroups();
        }
    }
}
