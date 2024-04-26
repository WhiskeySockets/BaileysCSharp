using BaileysCSharp.LibSignal;
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.Sessions
{
    public class CurrentRatchet
    {
        [JsonPropertyName("ephemeralKeyPair")]
        public KeyPair EphemeralKeyPair { get; set; }

        [JsonPropertyName("lastRemoteEphemeralKey")]
        public byte[] LastRemoteEphemeralKey { get; set; }

        [JsonPropertyName("previousCounter")]
        public int PreviousCounter { get; set; }

        [JsonPropertyName("rootKey")]
        public byte[] RootKey { get; set; }
    }
}
