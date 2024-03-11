using LiteDB;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    [FolderPrefix("key-pair")]
    public class PreKeyPair : KeyPair
    {
        public PreKeyPair(string id, KeyPair? key)
        {
            Id = id;
            Public = key.Public;
            Private = key.Private;
        }

        public PreKeyPair()
        {
            
        }

        [BsonId]
        public string Id { get; }
    }
}
