
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.Sessions
{
    public class Chain
    {
        public Chain()
        {
            MessageKeys = new Dictionary<int, byte[]>();
        }

        [JsonPropertyName("chainKey")]
        public ChainKey ChainKey { get; set; }

        [JsonPropertyName("chainType")]
        public int ChainType { get; set; }

        [JsonPropertyName("messageKeys")]
        public Dictionary<int,byte[]> MessageKeys { get; set; }
    }
}
