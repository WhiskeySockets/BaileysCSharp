using Proto;

namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IDeleteable
    {
        public MessageKey Delete { get; set; }
    }
}
