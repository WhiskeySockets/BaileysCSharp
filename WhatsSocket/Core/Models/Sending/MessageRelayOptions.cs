using BaileysCSharp.Core.Models.Sending.Interfaces;

namespace BaileysCSharp.Core.Models.Sending
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
