using Newtonsoft.Json;

namespace WhatsSocket.Core.Models.SenderKeys
{
    public class SenderSigningKeyStructure
    {
        [JsonProperty("public")]
        public byte[] Public { get; set; }
    }


}
