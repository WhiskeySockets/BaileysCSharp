using Newtonsoft.Json;
using System.Text.RegularExpressions;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.NoSQL;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace WhatsSocket.Core.Stores
{


    public class KeyStore
    {

        [JsonProperty("keys")]
        public Dictionary<int, KeyPair> Keys { get; set; }
        public ConfigStore Config { get; }
        public EventEmitter Ev { get; }

        private string _keyStore;
        public KeyStore(string path, ConfigStore config, EventEmitter ev)
        {
            _keyStore = path;
            Config = config;
            Ev = ev;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<int, KeyPair>();
            var files = Directory.GetFiles(_keyStore, "*.json");
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("pre-key-"))
                {
                    var id = fileInfo.Name.Replace(".json", "").Replace("pre-key-", "");
                    if (File.Exists(item))
                    {
                        Keys[Convert.ToInt32(id)] = KeyPair.Deserialize(File.ReadAllText(item));
                    }
                }
            }

        }

        public void Set(Dictionary<int, KeyPair> pairs)
        {
            foreach (var item in pairs)
            {
                Keys[item.Key] = item.Value;
                Config.Set(item.Key.ToString(), new KeyPairModel(item.Key.ToString(), item.Value));
                File.WriteAllText(Path.Combine(_keyStore, $"pre-key-{item.Key}.json"), JsonConvert.SerializeObject(item.Value));
            }
            Ev.Emit(this);
        }
        public void Set(int id, KeyPair? key)
        {
            if (key == null)
            {
                Keys.Remove(id);
                Config.Set<KeyPairModel>(id.ToString(), null);
                //File.Copy(Path.Combine(_keyStore, $"pre-key-{id}.json"), Path.Combine(_keyStore, $"pre-key-{id}.used"));
            }
            else
            {
                Keys[id] = key;
                Config.Set(id.ToString(), new KeyPairModel(id.ToString(), key));
                File.WriteAllText(Path.Combine(_keyStore, $"pre-key-{id}.json"), JsonConvert.SerializeObject(key));
            }


            Ev.Emit(this);
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

        internal KeyPair? Get(int id)
        {
            var existing = Config.Get<KeyPairModel>((id + 1).ToString());
            if (Keys.ContainsKey(id))
                return Keys[id];
            return null;
        }

    }


}
