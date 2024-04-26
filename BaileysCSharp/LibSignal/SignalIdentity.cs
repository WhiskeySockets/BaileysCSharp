using BaileysCSharp.Core.Signal;
using System.Text.Json.Serialization;

namespace BaileysCSharp.LibSignal
{
    public class SignalIdentity
    {
        [JsonPropertyName("identifier")]
        public ProtocolAddress Identifier { get; set; }
        [JsonPropertyName("identifierKey")]
        public byte[] IdentifierKey { get; set; }
    }

}
