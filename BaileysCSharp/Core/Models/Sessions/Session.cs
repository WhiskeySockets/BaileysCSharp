using BaileysCSharp.Core.Helper;
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.Sessions
{
    public class Session
    {
        public Session()
        {
            Chains = new Dictionary<string, Chain>();
        }
        [JsonPropertyName("registrationId")]
        public uint RegistrationId { get; set; }

        [JsonPropertyName("currentRatchet")]
        public CurrentRatchet CurrentRatchet { get; set; }

        [JsonPropertyName("indexInfo")]
        public IndexInfo IndexInfo { get; set; }

        [JsonPropertyName("_chains")]
        public Dictionary<string, Chain> Chains { get; set; }

        [JsonPropertyName("pendingPreKey")]
        public PendingPreKey PendingPreKey { get; set; }

        internal void DeleteChain(byte[] remoteKey)
        {
            Chains.Remove(remoteKey.ToBase64());
        }

        public Chain? GetChain(byte[] remoteKey)
        {
            if (Chains.TryGetValue(remoteKey.ToBase64(), out var value))
            {
                return value;
            }
            return null;
        }
    }

    public class E2ESession
    {
        public uint RegistrationId { get; set; }
        public byte[] IdentityKey { get; set; }
        public SignedPreKey SignedPreKey { get; set; }
        public PreKeyPair PreKey { get; set; }
    }
}
