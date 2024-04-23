using LiteDB;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.LibSignal;

namespace BaileysCSharp.Core.Models
{
    [FolderPrefix("pre-key")]
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
