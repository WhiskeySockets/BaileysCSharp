using BaileysCSharp.Core.Converters;
using BaileysCSharp.Core.Helper;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BaileysCSharp.LibSignal
{
    public class KeyPair
    {
        [JsonPropertyName("private")]
        public byte[] Private { get; set; }

        [JsonPropertyName("public")]
        public byte[] Public { get; set; }

        internal static KeyPair Deserialize(string data)
        {
            try
            {
                return JsonSerializer.Deserialize<KeyPair>(data, JsonHelper.Options);
            }
            catch (Exception)
            {
                return JsonSerializer.Deserialize<KeyPair>(data, JsonHelper.BufferOptions);
            }
        }
    }
}
