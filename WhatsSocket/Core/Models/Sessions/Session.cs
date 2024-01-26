using Google.Protobuf;
using Newtonsoft.Json;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Models.Sessions
{
    public class Session
    {
        public Session()
        {
            Chains = new Dictionary<string, Chain>();
        }
        [JsonProperty("registrationId")]
        public uint RegistrationId { get; set; }

        [JsonProperty("currentRatchet")]
        public CurrentRatchet CurrentRatchet { get; set; }

        [JsonProperty("indexInfo")]
        public IndexInfo IndexInfo { get; set; }

        [JsonProperty("_chains")]
        public Dictionary<string, Chain> Chains { get; set; }

        internal void DeleteChain(byte[] remoteKey)
        {
            if (Chains.ContainsKey(remoteKey.ToBase64()))
            {
                Chains.Remove(remoteKey.ToBase64());
            }
        }

        internal Chain? GetChain(byte[] remoteKey)
        {
            if (Chains.ContainsKey(remoteKey.ToBase64()))
            {
                return Chains[remoteKey.ToBase64()];
            }
            return null;
        }
    }
}
