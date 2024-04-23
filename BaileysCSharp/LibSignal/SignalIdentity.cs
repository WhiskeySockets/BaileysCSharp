using BaileysCSharp.Core.Signal;
using Newtonsoft.Json;

namespace BaileysCSharp.LibSignal
{
    public class SignalIdentity
    {
        [JsonProperty("identifier")]
        public ProtocolAddress Identifier { get; set; }
        [JsonProperty("identifierKey")]
        public byte[] IdentifierKey { get; set; }
    }

}
