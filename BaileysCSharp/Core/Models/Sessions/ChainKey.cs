using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.Sessions
{
    public class ChainType
    {
        public const int SENDING = 1;
        public const int RECEIVING = 2;
    }

    public class BaseKeyType
    {
        public const int OURS = 1;
        public const int THEIRS = 2;
    }

    public class ChainKey
    {
        [JsonPropertyName("counter")]
        public int Counter { get; set; }

        [JsonPropertyName("key")]
        public byte[] Key { get; set; }
    }
}
