using Newtonsoft.Json;

namespace BaileysCSharp.Core.Models.SenderKeys
{
    public class SenderChainKeyStructure
    {
        [JsonProperty("iteration")]
        public uint Iteration { get; set; }

        [JsonProperty("seed")]
        public byte[] Seed { get; set; }

    }


}
