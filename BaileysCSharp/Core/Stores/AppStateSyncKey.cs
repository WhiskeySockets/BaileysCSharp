using Proto;
using BaileysCSharp.Core.NoSQL;
using System.Text.Json.Serialization;
using System.Text.Json;
using BaileysCSharp.Core.Helper;

namespace BaileysCSharp.Core.Stores
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

        [JsonPropertyName("keyData")]
        public byte[] KeyData { get; set; }

        [JsonPropertyName("fingerprint")]
        public Fingerprint Fingerprint { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        internal string? Serialize()
        {
            return JsonSerializer.Serialize(this, JsonHelper.Options);
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

        [JsonPropertyName("rawId")]
        public uint RawId { get; set; }

        [JsonPropertyName("currentIndex")]
        public uint CurrentIndex { get; set; }

        [JsonPropertyName("deviceIndexes")]
        public List<uint> DeviceIndexes { get; set; }
    }
}
