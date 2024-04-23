using Proto;
using BaileysCSharp.Core.Models.Sending.Interfaces;

namespace BaileysCSharp.Core.Models.Sending.NonMedia
{
    public class TextMessageContent : AnyMessageContent, IMentionable, IContextable, IEditable
    {
        public string Text { get; set; }
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }
    }
}
