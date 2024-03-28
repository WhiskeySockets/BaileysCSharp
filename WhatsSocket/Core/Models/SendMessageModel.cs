using Proto;

namespace WhatsSocket.Core.Models
{
    // types to generate WA messages
    public class AnyContentMessageModel
    {
        public bool? DisappearingMessagesInChat { get; set; }   

    }


    public class MessageGenerationOptionsFromContent : MessageGenerationOptions
    {
        public string? UserJid { get; set; }
    }

    public class MessageGenerationOptions: MinimalRelayOptions
    {
        public ulong Timestamp { get; set; }
        public WebMessageInfo Quoted {  get; set; }

        public ulong? EphemeralExpiration { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public List<string>? StatusJidList { get; set; }

        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }

    public class ExtendedTextMessageModel : AnyContentMessageModel
    {
        public string Text { get; set; }
    }

    public class MessageParticipant
    {
        public string Jid { get; set; }
        public ulong Count { get; set; }
    }

    public class MessageRelayOptions : MinimalRelayOptions
    {
        public MessageParticipant Participant { get; set; }
        public Dictionary<string, string> AdditionalAttributes { get; set; }
        public bool? UseUserDevicesCache { get; set; }

        public List<string>? StatusJidList { get; set; }

    }

    public class MinimalRelayOptions
    {
        public string MessageID { get; set; }
    }
}
