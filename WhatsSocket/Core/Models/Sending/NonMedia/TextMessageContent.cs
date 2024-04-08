using Proto;
using WhatsSocket.Core.Models.Sending.Interfaces;

namespace WhatsSocket.Core.Models.Sending.NonMedia
{
    public class TextMessageContent : AnyMessageContent, IMentionable, IContextable, IEditable
    {
        public string Text { get; set; }
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }
    }
}
