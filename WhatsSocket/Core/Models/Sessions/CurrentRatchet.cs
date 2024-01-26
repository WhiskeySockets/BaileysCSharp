using Newtonsoft.Json;

namespace WhatsSocket.Core.Models.Sessions
{
    public class CurrentRatchet
    {
        [JsonProperty("ephemeralKeyPair")]
        public KeyPair EphemeralKeyPair { get; set; }

        [JsonProperty("lastRemoteEphemeralKey")]
        public byte[] LastRemoteEphemeralKey { get; set; }

        [JsonProperty("previousCounter")]
        public int PreviousCounter { get; set; }

        [JsonProperty("rootKey")]
        public byte[] RootKey { get; set; }
    }
}
