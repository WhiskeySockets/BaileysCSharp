using Proto;
using BaileysCSharp.Core.Models.Sending.Interfaces;

namespace BaileysCSharp.Core.Models.Sending.Media
{
    public abstract class AnyMediaMessageContent : AnyMessageContent, IMentionable, IContextable, IEditable
    {
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }
        public string Property { get; set; }

        public abstract IMediaMessage ToMediaMessage();


        public abstract Task Process();

    }
}
