using Newtonsoft.Json;
using Proto;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Stores
{
    public class SenderKeyStore
    {

        public Dictionary<string, SenderKeyRecord> Keys { get; set; }
        public EventEmitter Ev { get; }

        private string _keyStore;

        public SenderKeyStore(string path, EventEmitter ev)
        {
            _keyStore = path;
            Ev = ev;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<string, SenderKeyRecord>();
            var files = Directory.GetFiles(_keyStore);
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("sender-key-"))
                {
                    var id = fileInfo.Name.Replace("sender-key-", "").Replace(".json", "").Replace("-", ":");
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
                File.WriteAllText($"{_keyStore}/{filename}.json", key.Serialize());
            }
            Ev.Emit(this);
        }


        internal SenderKeyRecord Get(string id)
        {
            return Keys[id];
        }

    }
}
