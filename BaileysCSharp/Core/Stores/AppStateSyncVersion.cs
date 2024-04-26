using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Stores
{

    [FolderPrefix("app-state-sync-version")]
    public class AppStateSyncVersion
    {
        [JsonPropertyName("version")]
        public ulong Version { get; set; }

        [JsonPropertyName("hash")]
        public byte[] Hash { get; set; }


        [JsonPropertyName("indexValueMap")]
        public Dictionary<string, byte[]> IndexValueMap { get; set; }

        public AppStateSyncVersion()
        {
            IndexValueMap = new Dictionary<string, byte[]>();
            Version = 0;
            Hash = new byte[128];
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this, JsonHelper.Options);
        }
    }


}
