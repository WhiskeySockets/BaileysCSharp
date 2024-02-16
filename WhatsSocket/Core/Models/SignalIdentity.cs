using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class SignalIdentity
    {
        [JsonProperty("identifier")]
        public ProtocolAddress Identifier { get; set; }
        [JsonProperty("identifierKey")]
        public byte[] IdentifierKey { get; set; }
    }

}
