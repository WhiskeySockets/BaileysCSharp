using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.SenderKeys
{
    public class SenderSigningKeyStructure
    {
        [JsonPropertyName("public")]
        public byte[] Public { get; set; }
        [JsonPropertyName("private")]
        public byte[] Private { get; set; }
    }


}
