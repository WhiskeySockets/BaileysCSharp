using Proto;

namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IMiscMessageGenerationOptions : IMinimalRelayOptions, IMediaUpload
    {
        public ulong Timestamp { get; set; }
        public WebMessageInfo Quoted { get; set; }
        public ulong? EphemeralExpiration { get; set; }
        public List<string>? StatusJidList { get; set; }

    }
}
