using System.Text.Json.Serialization;
using static BaileysCSharp.Core.Utils.JidUtils;

namespace BaileysCSharp.Core.Signal
{
    public class ProtocolAddress
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("deviceId")]
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
