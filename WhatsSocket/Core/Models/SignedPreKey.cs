using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class SignedPreKey
    {
        [JsonProperty("keyPair")]
        public KeyPair KeyPair { get; set; }

        [JsonProperty("signature")]
        public byte[] Signature { get; set; }

        [JsonProperty("keyId")]
        public int KeyId { get; set; }
    }


}
