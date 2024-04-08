using WhatsSocket.Core.Models.Sending.Interfaces;

namespace WhatsSocket.Core.Models.Sending
{
    public class MessageRelayOptions : IMessageRelayOptions
    {
        public MessageParticipant Participant { get; set; }
        public Dictionary<string, string> AdditionalAttributes { get; set; }
        public bool? UseUserDevicesCache { get; set; }
        public List<string>? StatusJidList { get; set; }
        public string MessageID { get; set; }
    }
}
