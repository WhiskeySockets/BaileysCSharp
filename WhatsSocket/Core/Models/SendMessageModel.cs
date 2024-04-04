using Proto;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Models
{
    // types to generate WA messages
    public class AnyContentMessageModel
    {
        public bool? DisappearingMessagesInChat { get; set; }

    }

    public interface IContextable
    {
        ContextInfo? ContextInfo { get; set; }
    }

    public interface IMentionable
    {
        string[] Mentions { get; set; }
    }

    public interface IViewOnce
    {
        bool ViewOnce { get; set; }
    }

    public interface IEditable
    {
        public MessageKey? Edit { get; set; }
    }


    public class MessageGenerationOptionsFromContent : MiscMessageGenerationOptions
    {
        public string? UserJid { get; set; }

        public Logger Logger { get; set; }
    }

    public class MiscMessageGenerationOptions : MinimalRelayOptions
    {
        public ulong Timestamp { get; set; }
        public WebMessageInfo Quoted { get; set; }

        public ulong? EphemeralExpiration { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public List<string>? StatusJidList { get; set; }

        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }

    public class ExtendedTextMessageModel : AnyContentMessageModel, IMentionable, IContextable, IEditable
    {
        public string Text { get; set; }
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }
    }

    public class DeleteMessageModel : AnyContentMessageModel
    {
        public string RemoteJid { get; set; }
        public bool FormMe { get; set; }
    }

    public class MessageParticipant
    {
        public string Jid { get; set; }
        public ulong Count { get; set; }
    }

    public class MessageRelayOptions : MinimalRelayOptions
    {
        public MessageRelayOptions()
        {
            AdditionalAttributes = new Dictionary<string, string>();
        }
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
