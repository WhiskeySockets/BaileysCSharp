using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class SignedPreKey
    {
        [JsonProperty("keyPair")]
        public KeyPair KeyPair { get; set; }

        [JsonProperty("signature")]
        public byte[] Signature { get; set; }

        [JsonProperty("keyId")]
        public int KeyId { get; set; }
    }

    public delegate void KeyStoreChangeArgs(KeyStore store);

    public class KeyStore
    {
        public event KeyStoreChangeArgs OnStoreChange;
        public Dictionary<int, KeyPair> PreKeys { get; set; }

        public KeyStore()
        {
            PreKeys = new Dictionary<int, KeyPair>();
        }
        public KeyStore(Dictionary<int, KeyPair> load)
        {
            PreKeys = load;
        }


        public void Set(Dictionary<int, KeyPair> pairs)
        {
            foreach (var item in pairs)
            {
                PreKeys[item.Key] = item.Value;
            }
            OnStoreChange?.Invoke(this);
        }

        internal Dictionary<int,KeyPair> Range(List<int> keys)
        {
            Dictionary<int, KeyPair> pairs = new Dictionary<int, KeyPair>();
            foreach (var key in keys)
            {
                pairs[key] = PreKeys[key];
            }
            return pairs;
        }
    }
}
