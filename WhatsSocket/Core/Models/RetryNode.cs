using Proto;
using WhatsSocket.Exceptions;

namespace WhatsSocket.Core.Models
{
    public class RetryNode
    {
        public MessageKey Key { get; set; }
        public RetryMedia Media { get; set; }
        public Boom Error { get; set; }
    }

}