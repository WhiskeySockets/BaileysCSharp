using Newtonsoft.Json;

namespace BaileysCSharp.Core.Models.Sessions
{
    public class Chain
    {
        public Chain()
        {
            MessageKeys = new Dictionary<int, byte[]>();
        }

        [JsonProperty("chainKey")]
        public ChainKey ChainKey { get; set; }

        [JsonProperty("chainType")]
        public int ChainType { get; set; }

        [JsonProperty("messageKeys")]
        public Dictionary<int,byte[]> MessageKeys { get; set; }
    }
}
