using Proto;

namespace BaileysCSharp.Core.Models.Sending.NonMedia
{
    public class ReactMessageContent : IAnyMessageContent
    {
        public string ReactText { get; set; }
        public MessageKey Key { get; set; }
        public bool? DisappearingMessagesInChat { get; set; }

    }
}
