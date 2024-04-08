using Proto;
using WhatsSocket.Core.Models.Sending.Interfaces;

namespace WhatsSocket.Core.Models.Sending.Media
{
    public abstract class AnyMediaMessageContent : AnyMessageContent, IMentionable, IContextable, IEditable
    {
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }


        public abstract IMediaMessage ToMediaMessage();


        public abstract Task Process();
    }
}
