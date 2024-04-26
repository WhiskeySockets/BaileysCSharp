using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.Sessions
{
    public class IndexInfo
    {
        [JsonPropertyName("baseKey")]
        public byte[] BaseKey { get; set; }

        [JsonPropertyName("baseKeyType")]
        public int BaseKeyType { get; set; }

        [JsonPropertyName("closed")]
        public long Closed { get; set; }

        [JsonPropertyName("used")]
        public long Used { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("remoteIdentityKey")]
        public byte[] RemoteIdentityKey { get; set; }
    }


    public class PendingPreKey
    {
        [JsonPropertyName("signedKeyId")]
        public uint SignedKeyId { get; set; }
        [JsonPropertyName("preKeyId")]
        public uint PreKeyId { get; set; }
        [JsonPropertyName("baseKey")]
        public byte[]? BaseKey { get; set; }
    }
}
