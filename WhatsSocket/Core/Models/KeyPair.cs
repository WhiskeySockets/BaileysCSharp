using Newtonsoft.Json;

namespace WhatsSocket.Core.Credentials
{
    public class KeyPair
    {
        [JsonProperty("private")]
        public byte[] Private { get; set; }

        [JsonProperty("public")]
        public byte[] Public { get; set; }
    }



}
