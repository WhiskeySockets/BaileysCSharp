using LiteDB;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    public class ChatModel : IMayHaveID
    {
        public ChatModel()
        {
        }

        [BsonId]
        public string ID { get; set; }
        public ulong ConversationTimestamp { get; set; }
        public ulong LastMessageRecvTimestamp { get; set; }
        public ulong UnreadCount { get; set; }
        public bool ReadOnly { get; set; }
        public bool Archived { get; set; }

        public ulong EphemeralSettingTimestamp { get; set; }
        public ulong EphemeralExpiration { get; set; }
        public string Name { get; set; }

        public byte[] TcToken { get; set; }

        public string GetID()
        {
            return ID;
        }

        public override string ToString()
        {
            return $"{ID} - {Name}";
        }
    }

}