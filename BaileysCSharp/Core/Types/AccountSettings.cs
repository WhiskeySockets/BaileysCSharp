

using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Types
{
    public class AccountSettings
    {
        [JsonPropertyName("unarchiveChats")]
        public bool UnarchiveChats { get; set; }


        [JsonPropertyName("defaultDisappearingMode")]
        public DissapearingMode DefaultDissapearingMode { get; set; }
    }


    public class DissapearingMode
    {
        [JsonPropertyName("ephemeralExpiration")]
        public ulong EphemeralExpiration { get; set; }
        [JsonPropertyName("ephemeralSettingTimestamp")]
        public ulong EphemeralSettingTimestamp { get; set; }

    }


    public class MessagingHistory
    {

    }
}
