using Newtonsoft.Json;
using System.Text.RegularExpressions;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.SenderKeys;

namespace WhatsSocket.Core.Stores
{

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
