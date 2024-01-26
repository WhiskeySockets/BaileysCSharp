using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace WhatsSocket.Core.Models
{

    public delegate void KeyStoreChangeArgs(KeyStore store);

    public class KeyStore
    {
        public event KeyStoreChangeArgs OnStoreChange;


        [JsonProperty("keys")]
        public Dictionary<int, KeyPair> Keys { get; set; }

        private string _keyStore;
        public KeyStore(string path)
        {
            _keyStore = path;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<int, KeyPair>();
            var files = Directory.GetFiles(_keyStore);
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("pre-key-"))
                {
                    var id = Convert.ToInt32(fileInfo.Name.Replace(".json", "").Replace("pre-key-", ""));
                    if (File.Exists(item))
                    {
                        Keys[id] = KeyPair.Deserialize(File.ReadAllText(item));
                    }
                }
            }
        }

        public void Set(Dictionary<int, KeyPair> pairs)
        {
            foreach (var item in pairs)
            {
                Keys[item.Key] = item.Value;
                File.WriteAllText(Path.Combine(_keyStore, $"pre-key-{item.Key}.json"), JsonConvert.SerializeObject(item.Value));
            }
            OnStoreChange?.Invoke(this);
        }
        public void Set(int id, KeyPair? key)
        {
            if (key == null)
            {
                Keys.Remove(id);
                //File.Delete($"pre-key-{id}.json");
            }
            else
            {
                Keys[id] = key;
            }
            OnStoreChange?.Invoke(this);
        }

        internal Dictionary<int, KeyPair> Range(List<int> keys)
        {
            Dictionary<int, KeyPair> pairs = new Dictionary<int, KeyPair>();
            foreach (var key in keys)
            {
                pairs[key] = Keys[key];
            }
            return pairs;
        }

        internal KeyPair Get(int id)
        {
            return Keys[id];
        }

    }


    public delegate void SenderKeyStoreChangeArgs(SenderKeyStore store);

    public class SenderKeyStore
    {
        public event SenderKeyStoreChangeArgs OnSenderStoreChange;

        public Dictionary<string, SenderKey> Keys { get; set; }

        private string _keyStore;
        public SenderKeyStore(string path)
        {
            _keyStore = path;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<string, SenderKey>();
            var files = Directory.GetFiles(_keyStore);
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("sender-key-"))
                {
                    var id = fileInfo.Name.Replace(".json", "");
                    if (File.Exists(item))
                    {
                        Keys[id] = JsonConvert.DeserializeObject<SenderKey>(File.ReadAllText(item));
                    }
                }
            }
        }

        public void Set(string id, SenderKey? key)
        {
            if (key == null)
            {
                Keys.Remove(id);
                //File.Delete($"pre-key-{id}.json");
            }
            else
            {
                Keys[id] = key;
            }
            OnSenderStoreChange?.Invoke(this);
        }


        internal SenderKey Get(string id)
        {
            return Keys[id];
        }

    }
}
