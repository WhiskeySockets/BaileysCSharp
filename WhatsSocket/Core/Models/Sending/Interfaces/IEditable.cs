using Proto;

namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IEditable
    {
        public MessageKey? Edit { get; set; }
    }
}
