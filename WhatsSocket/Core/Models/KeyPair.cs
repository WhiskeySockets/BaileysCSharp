using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
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
