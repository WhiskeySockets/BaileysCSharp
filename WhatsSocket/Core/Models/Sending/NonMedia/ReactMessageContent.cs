using Proto;

namespace WhatsSocket.Core.Models.Sending.NonMedia
{
    public class ReactMessageContent : AnyMessageContent
    {
        public string ReactText { get; set; }
        public MessageKey Key { get; set; }

    }
}
