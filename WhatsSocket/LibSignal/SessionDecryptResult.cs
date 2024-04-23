using BaileysCSharp.Core.Models.Sessions;

namespace BaileysCSharp.LibSignal
{
    public class SessionDecryptResult
    {
        public Session Session { get; set; }
        public byte[] PlainText { get; set; }
    }

}