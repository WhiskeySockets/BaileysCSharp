using Newtonsoft.Json;
using WhatsSocket.Core.Delegates;

namespace WhatsSocket.Core.Stores
{
    public class AppStateSyncKeyStore
    {
        public Dictionary<string, AppStateSyncKeyStructure> Keys { get; set; }
        public EventEmitter Ev { get; }

        private string _keyStore;
        public AppStateSyncKeyStore(string path, EventEmitter ev)
        {
            _keyStore = path;
            Ev = ev;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<string, AppStateSyncKeyStructure>();
            var files = Directory.GetFiles(_keyStore);
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("app-state-sync-key-"))
                {
                    var id = fileInfo.Name.Replace("app-state-sync-key-", "").Replace(".json", "").Replace("-", ":");
                    id = id.Replace("__", "/");
                    if (File.Exists(item))
                    {
                        Keys[id] = JsonConvert.DeserializeObject<AppStateSyncKeyStructure>(File.ReadAllText(item));
                    }
                }
            }
        }

        public void Set(string id, AppStateSyncKeyStructure? key)
        {
            id = id.Replace("/", "__");
            if (key == null)
            {
                Keys.Remove(id);
                //File.Copy(Path.Combine(_keyStore, $"sender-key-{id}.json"), Path.Combine(_keyStore, $"sender-key-{id}.used"));
                //File.Delete($"pre-key-{id}.json");
            }
            else
            {
                Keys[id] = key;
                var filename = $"app-state-sync-key-{id}";
                File.WriteAllText($"{_keyStore}/{filename}.json", key.Serialize());
            }
            Ev.Emit(this);
        }


        internal AppStateSyncKeyStructure? Get(string id)
        {
            if (Keys.ContainsKey(id))
                return Keys[id];
            return null;
        }

        internal Dictionary<string, AppStateSyncKeyStructure> Get(params string[] ids)
        {
            var result = new Dictionary<string, AppStateSyncKeyStructure>();
            foreach (var id in ids)
            {
                if (Keys.ContainsKey(id))
                {
                    result[id] = Keys[id];
                }
            }
            return result;
        }

    }


}
