using Proto;

namespace WhatsSocket.Core.Models
{

    public class ChatMutation
    {
        public SyncActionData SyncAction { get; set; }
        public string[] Index { get; set; }
    }

}