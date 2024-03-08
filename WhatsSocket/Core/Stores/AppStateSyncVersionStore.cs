using Newtonsoft.Json;
using WhatsSocket.Core.Delegates;

namespace WhatsSocket.Core.Stores
{
    public class AppStateSyncVersionStore
    {
        public Dictionary<string, AppStateSyncVersion> Keys { get; set; }
        public EventEmitter Ev { get; }

        private string _keyStore;
        public AppStateSyncVersionStore(string path, EventEmitter ev)
        {
            _keyStore = path;
            Ev = ev;
            Directory.CreateDirectory(_keyStore);
            Keys = new Dictionary<string, AppStateSyncVersion>();
            var files = Directory.GetFiles(_keyStore);
            foreach (var item in files)
            {
                var fileInfo = new FileInfo(item);
                if (fileInfo.Name.StartsWith("app-state-sync-version-"))
                {
                    var id = fileInfo.Name.Replace("app-state-sync-version-", "").Replace(".json", "").Replace("-", ":");
                    if (File.Exists(item))
                    {
                        Keys[id] = JsonConvert.DeserializeObject<AppStateSyncVersion>(File.ReadAllText(item));
                    }
                }
            }
        }

        public void Set(string id, AppStateSyncVersion? key)
        {
            if (key == null)
            {
                Keys.Remove(id);
                var filename = $"app-state-sync-version-{id}";
                if (File.Exists($"{_keyStore}/{filename}.json"))
                {
                    File.Delete($"{_keyStore}/{filename}.json");
                }
            }
            else
            {
                Keys[id] = key;
                var filename = $"app-state-sync-version-{id}";
                File.WriteAllText($"{_keyStore}/{filename}.json", key.Serialize());
            }
            Ev.Emit(this);
        }


        internal AppStateSyncVersion? Get(string id)
        {
            if (Keys.ContainsKey(id))
                return Keys[id];
            return null;
        }

        internal Dictionary<string, AppStateSyncVersion> Get(params string[] ids)
        {
            var result = new Dictionary<string, AppStateSyncVersion>();
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
