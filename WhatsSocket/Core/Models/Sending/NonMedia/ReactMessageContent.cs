using Proto;

namespace WhatsSocket.Core.Models.Sending.NonMedia
{
    public class ReactMessageContent : IAnyMessageContent
    {
        public string ReactText { get; set; }
        public MessageKey Key { get; set; }
        public bool? DisappearingMessagesInChat { get; set; }

    }
}
