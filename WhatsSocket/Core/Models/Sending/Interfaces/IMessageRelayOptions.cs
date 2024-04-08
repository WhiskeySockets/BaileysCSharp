namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IMessageRelayOptions : IMinimalRelayOptions
    {
        public MessageParticipant Participant { get; set; }
        public Dictionary<string, string> AdditionalAttributes { get; set; }
        public bool? UseUserDevicesCache { get; set; }

        public List<string>? StatusJidList { get; set; }
    }
}
