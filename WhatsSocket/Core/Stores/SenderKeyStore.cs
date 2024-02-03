using Newtonsoft.Json;
using Proto;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Models.SenderKeys;

namespace WhatsSocket.Core.Stores
{
    public class SenderKeyStore
    {
        public event SenderKeyStoreChangeArgs OnSenderStoreChange;

        public Dictionary<string, SenderKeyRecord> Keys { get; set; }

        private string _keyStore;
        public SenderKeyStore(string path)
        {
            _keyStore = path;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<string, SenderKeyRecord>();
            var files = Directory.GetFiles(_keyStore);
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("sender-key-"))
                {
                    var id = fileInfo.Name.Replace("sender-key-","").Replace(".json", "").Replace("-", ":");
                    if (File.Exists(item))
                    {
                        Keys[id] = new SenderKeyRecord(item);
                    }
                }
            }
        }

        public void StoreSenderKey(string id, SenderKeyRecord? key)
        {
            if (key == null)
            {
                Keys.Remove(id);
                //File.Copy(Path.Combine(_keyStore, $"sender-key-{id}.json"), Path.Combine(_keyStore, $"sender-key-{id}.used"));
                //File.Delete($"pre-key-{id}.json");
            }
            else
            {
                Keys[id] = key;
                var filename = $"sender-key-{id.Replace(":", "-")}";
                File.WriteAllText($"{_keyStore}/{filename}.json", key.Serialize()) ;
            }
            OnSenderStoreChange?.Invoke(this);
        }


        internal SenderKeyRecord Get(string id)
        {
            return Keys[id];
        }

    }
}
