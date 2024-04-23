using Proto;
using BaileysCSharp.Core.Models.Sending.Interfaces;

namespace BaileysCSharp.Core.Models.Sending.NonMedia
{
    public class DeleteMessageContent : AnyMessageContent, IDeleteable
    {
        public MessageKey? Delete { get; set; }
    }
}
