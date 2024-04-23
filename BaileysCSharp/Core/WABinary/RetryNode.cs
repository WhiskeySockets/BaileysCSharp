using Proto;
using BaileysCSharp.Exceptions;

namespace BaileysCSharp.Core.WABinary
{
    public class RetryNode
    {
        public MessageKey Key { get; set; }
        public RetryMedia Media { get; set; }
        public Boom Error { get; set; }
    }

}