using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;
using static WhatsSocket.Core.Utils.ChatUtils;
using static WhatsSocket.Core.Models.ChatConstants;
using static WhatsSocket.Core.Utils.GenericUtils;

namespace WhatsSocket.Core.Sockets
{
    public abstract class GroupSocket : ChatSocket
    {
        public GroupSocket([NotNull] SocketConfig config) : base(config)
        {

        }


        protected override async Task<bool> HandleDirtyUpdate(BinaryNode node)
        {
            var dirtyNode = GetBinaryNodeChild(node, "dirty");
            if (dirtyNode?.getattr("type") == "groups")
            {
                await GroupFetchAllParticipating();
                await CleanDirtyBits("groups");
            }

            return true;
        }

        private async Task GroupFetchAllParticipating()
        {
            ///TODO:
        }
    }
}
