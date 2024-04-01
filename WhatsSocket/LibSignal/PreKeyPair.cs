using LiteDB;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.LibSignal;

namespace WhatsSocket.Core.Models
{
    [FolderPrefix("key-pair")]
    public class PreKeyPair : KeyPair
    {
        public PreKeyPair(uint id, KeyPair? key)
        {
            KeyId = id;
            Public = key.Public;
            Private = key.Private;
        }

        public PreKeyPair()
        {
            
        }

        [BsonId]
        public uint KeyId { get; set; }
    }
}
