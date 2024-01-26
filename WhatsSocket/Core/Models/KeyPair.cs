using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class KeyPair
    {
        [JsonProperty("private")]
        public byte[] Private { get; set; }

        [JsonProperty("public")]
        public byte[] Public { get; set; }

        internal static KeyPair Deserialize(string data)
        {
            try
            {
                return JsonConvert.DeserializeObject<KeyPair>(data);
            }
            catch (Exception)
            {
                return JsonConvert.DeserializeObject<KeyPair>(data,new BufferConverter());
            }
        }
    }
    public class SenderKey
    {
        [JsonProperty("senderKeyId")]
        public int SenderKeyId { get; set; }

        [JsonProperty("senderChainKey")]
        public SenderChainKey SenderChainKey { get; set; }

        [JsonProperty("senderSigningKey")]
        public SenderSigningKey SenderSigningKey { get; set; }
    }

    public class SenderChainKey
    {
        [JsonProperty("iteration")]
        public int Iteration { get; set; }

        [JsonProperty("seed")]
        public byte[] Seed { get; set; }
    }

    public class SenderSigningKey
    {
        [JsonProperty("public")]
        public byte[] Public { get; set; }
    }

}
