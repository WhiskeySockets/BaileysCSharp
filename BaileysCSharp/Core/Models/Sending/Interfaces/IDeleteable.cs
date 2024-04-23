using Proto;

namespace BaileysCSharp.Core.Models.Sending.Interfaces
{
    public interface IDeleteable
    {
        public MessageKey Delete { get; set; }
    }
}
