using Proto;

namespace BaileysCSharp.Core.Models.Sending.Interfaces
{
    public interface IEditable
    {
        public MessageKey? Edit { get; set; }
    }
}
