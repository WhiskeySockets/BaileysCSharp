using Newtonsoft.Json;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Converters;

namespace BaileysCSharp.LibSignal
{
    public class KeyPair
    {
        [JsonProperty("private")]
        public byte[] Private { get; set; }

        [JsonProperty("public")]
        public byte[] Public { get; set; }

        internal static KeyPair Deserialize(string data)
        {
            try
            {
                return JsonConvert.DeserializeObject<KeyPair>(data);
            }
            catch (Exception)
            {
                return JsonConvert.DeserializeObject<KeyPair>(data, new BufferConverter());
            }
        }
    }
}
