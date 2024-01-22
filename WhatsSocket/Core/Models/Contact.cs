using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class Contact
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("lid")]
        public string LID { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("notify")]
        public string Notify { get; set; }

        [JsonProperty("verifiedName")]
        public string VerifiedName { get; set; }

        [JsonProperty("imgUrl")]
        public string ImgUrl { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
    

    public class ProtocolAddress
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deviceId")]
        public long DeviceID { get; set; }
    }

    public class SignalIdentity
    {
        [JsonProperty("identifier")]
        public ProtocolAddress Identifier { get; set; }
        [JsonProperty("identifierKey")]
        public byte[] IdentifierKey { get; set; }
    }

}
