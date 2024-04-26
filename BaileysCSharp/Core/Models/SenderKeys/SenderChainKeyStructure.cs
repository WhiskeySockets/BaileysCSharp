
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.SenderKeys
{
    public class SenderChainKeyStructure
    {
        [JsonPropertyName("iteration")]
        public uint Iteration { get; set; }

        [JsonPropertyName("seed")]
        public byte[] Seed { get; set; }

    }


}
