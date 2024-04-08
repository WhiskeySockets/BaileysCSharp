using Proto;
using WhatsSocket.Core.Models.Sending.Interfaces;

namespace WhatsSocket.Core.Models.Sending.NonMedia
{
    public class DeleteMessageContent : AnyMessageContent, IDeleteable
    {
        public MessageKey? Delete { get; set; }
    }
}
