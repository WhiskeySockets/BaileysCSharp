using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class AccountSettings
    {
        [JsonProperty("unarchiveChats")]
        public bool UnarchiveChats { get; set; }


        [JsonProperty("defaultDisappearingMode")]
        public DissapearingMode DefaultDissapearingMode { get; set; }
    }


    public class DissapearingMode
    {
        [JsonProperty("ephemeralExpiration")]
        public ulong EphemeralExpiration { get; set; }
        [JsonProperty("ephemeralSettingTimestamp")]
        public ulong EphemeralSettingTimestamp { get; set; }

    }


    public class MessagingHistory
    {

    }
}
