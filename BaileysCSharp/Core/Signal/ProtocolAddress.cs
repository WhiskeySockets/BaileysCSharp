using Newtonsoft.Json;
using static BaileysCSharp.Core.Utils.JidUtils;

namespace BaileysCSharp.Core.Signal
{
    public class ProtocolAddress
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deviceId")]
        public long DeviceID { get; set; }

        public ProtocolAddress()
        {

        }
        public ProtocolAddress(string jid) : this(JidDecode(jid))
        {

        }

        public ProtocolAddress(FullJid jid)
        {
            Name = jid.User;
            DeviceID = jid.Device ?? 0;
        }

        public override string ToString()
        {
            return $"{Name}.{DeviceID}";
        }
    }

}
