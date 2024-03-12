using Proto;

namespace WhatsSocket.Core.Models
{
    public class MessageUpdateModel
    {
        public Message? Message { get; set; }
        public WebMessageInfo.Types.StubType MessageStubType { get; set; }
        public MessageKey Key { get; set; }
    }

}
