using Newtonsoft.Json;
using System.Text.RegularExpressions;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Stores
{

    [FolderPrefix("app-state-sync-version")]
    public class AppStateSyncVersion
    {
        [JsonProperty("version")]
        public ulong Version { get; set; }

        [JsonProperty("hash")]
        public byte[] Hash { get; set; }


        [JsonProperty("indexValueMap")]
        public Dictionary<string, byte[]> IndexValueMap { get; set; }

        public AppStateSyncVersion()
        {
            IndexValueMap = new Dictionary<string, byte[]>();
            Version = 0;
            Hash = new byte[128];
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }


}
