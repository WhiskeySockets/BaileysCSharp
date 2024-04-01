using WhatsSocket.Core.Models.Sessions;

namespace WhatsSocket.LibSignal
{
    public class SessionDecryptResult
    {
        public Session Session { get; set; }
        public byte[] PlainText { get; set; }
    }

}