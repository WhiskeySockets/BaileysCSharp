using Newtonsoft.Json;
using WhatsSocket.LibSignal;

namespace WhatsSocket.Core.Models
{
    public class SignedPreKey : PreKeyPair
    {
        public byte[] Signature { get; set; }
    }
    
    //For Compatibility Conversion
    public class SignedPreKeyModel
    {
        [JsonProperty("keyPair")]
        public KeyPair KeyPair { get; set; }

        [JsonProperty("signature")]
        public byte[] Signature { get; set; }

        [JsonProperty("keyId")]
        public ulong KeyId { get; set; }
    }


}
