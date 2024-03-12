using Proto;

namespace WhatsSocket.Core.Models
{
    public class MessageUpdate
    {
        public MessageKey Key { get; set; }
        public MessageUpdateModel Update { get; set; }

        public static MessageUpdate FromRevoke(WebMessageInfo message, Message.Types.ProtocolMessage protocolMsg)
        {
            var result = new MessageUpdate();
            result.Key = message.Key;
            result.Key.Id = protocolMsg.Key.Id;

            result.Update = new MessageUpdateModel();
            result.Update.Message = null;
            result.Update.MessageStubType = message.MessageStubType;
            result.Update.Key = message.Key;

            return result;
        }
    }

}
