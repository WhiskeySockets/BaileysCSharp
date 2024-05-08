using Proto;

namespace BaileysCSharp.Core.Models.Sending.Interfaces
{
    public interface IEditable
    {
        public MessageKey? Edit { get; set; }
    }
    public interface IButtonable
    {
        public Message.Types.ButtonsMessage.Types.Button?[] Buttons { get; set; }
    }
}
