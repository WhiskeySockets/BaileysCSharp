using Newtonsoft.Json;

namespace WhatsSocket.Core.Credentials
{
    public class AccountSettings
    {
        [JsonProperty("unarchiveChats")]
        public bool UnarchiveChats { get; set; }
    }



}
