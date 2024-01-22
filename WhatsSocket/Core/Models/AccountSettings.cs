using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class AccountSettings
    {
        [JsonProperty("unarchiveChats")]
        public bool UnarchiveChats { get; set; }
    }



}
