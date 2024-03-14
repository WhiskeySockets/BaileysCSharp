using Google.Protobuf;
using LiteDB;
using Proto;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    public class MessageModel : IMayHaveID
    {
        [BsonId]
        public string ID { get; set; }

        public string MessageType { get; set; }
        public string RemoteJid { get; set; }

        public byte[] Message { get; set; }
        public bool FromMe { get; internal set; }

        public MessageModel()
        {

        }

        public MessageModel(WebMessageInfo info)
        {
            ID = info.Key.Id;
            MessageType = info.MessageStubType.ToString();
            RemoteJid = info.Key.RemoteJid;
            Message = info.ToByteArray();
            FromMe = info.Key.FromMe;
        }

        public string GetID()
        {
            return ID;
        }

        public WebMessageInfo ToMessageInfo()
        {
            return WebMessageInfo.Parser.ParseFrom(Message);
        }
    }
}
