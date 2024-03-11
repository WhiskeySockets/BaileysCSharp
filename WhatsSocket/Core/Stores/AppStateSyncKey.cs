using Newtonsoft.Json;
using Proto;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Stores
{
    [FolderPrefix("app-state-sync-key")]
    public class AppStateSyncKeyStructure
    {
        public AppStateSyncKeyStructure(Message.Types.AppStateSyncKeyData data)
        {
            KeyData = data.KeyData.ToByteArray();
            Timestamp = data.Timestamp;
            Fingerprint = new Fingerprint(data.Fingerprint);
        }

        public AppStateSyncKeyStructure()
        {

        }

        [JsonProperty("keyData")]
        public byte[] KeyData { get; set; }

        [JsonProperty("fingerprint")]
        public Fingerprint Fingerprint { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
        internal string? Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Fingerprint
    {
        public Fingerprint()
        {

        }
        public Fingerprint(Message.Types.AppStateSyncKeyFingerprint fingerprint)
        {
            RawId = fingerprint.RawId;
            CurrentIndex = fingerprint.CurrentIndex;
            DeviceIndexes = fingerprint.DeviceIndexes.ToList();
        }

        [JsonProperty("rawId")]
        public uint RawId { get; set; }

        [JsonProperty("currentIndex")]
        public uint CurrentIndex { get; set; }

        [JsonProperty("deviceIndexes")]
        public List<uint> DeviceIndexes { get; set; }
    }
}
