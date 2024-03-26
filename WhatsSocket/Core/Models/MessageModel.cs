using Google.Protobuf;
using LiteDB;
using Proto;
using WhatsSocket.Core.NoSQL;
using static Proto.Message.Types;

namespace WhatsSocket.Core.Models
{
    public enum MessageUpsertType
    {
        Append = 1,
        Notify = 2
    }

    public class GroupUpdateModel
    {
        public GroupUpdateModel(string jid, GroupMetadataModel update)
        {
            Jid = jid;
            Update = update;
        }

        public string Jid { get; }
        public GroupMetadataModel Update { get; }
    }

    public class GroupParticipantUpdateModel
    {
        public GroupParticipantUpdateModel(string jid, string participant, string action)
        {
            Jid = jid;
            Participant = participant;
            Action = action;
        }

        public string Jid { get; }
        public string Participant { get; }
        public string Action { get; }
    }

    public class MessageReactionModel
    {
        public MessageReactionModel(Message.Types.ReactionMessage reactionMessage, MessageKey key)
        {
            ReactionMessage = reactionMessage;
            Key = key;
        }

        public Message.Types.ReactionMessage ReactionMessage { get; }
        public MessageKey Key { get; }
    }

    public class MessageHistoryModel
    {
        public MessageHistoryModel(List<ContactModel> contacts, List<ChatModel> chats, List<WebMessageInfo> messages, bool islatest)
        {
            Contacts = contacts;
            Chats = chats;
            Messages = messages;
            IsLatest = islatest;
        }

        public List<ContactModel> Contacts { get; set; }
        public List<ChatModel> Chats { get; set; }
        public List<WebMessageInfo> Messages { get; set; }
        public bool IsLatest { get; set; }
    }

    public class MessageUpsertModel
    {
        public MessageUpsertModel(MessageUpsertType type, params WebMessageInfo[] messages)
        {
            Type = type;
            Messages = messages;
        }

        public MessageUpsertType Type { get; set; }
        public WebMessageInfo[] Messages { get; set; }
    }

    public class MessageModel : IMayHaveID
    {
        [BsonId]
        public string ID { get; set; }

        public string MessageType { get; set; }
        public string RemoteJid { get; set; }

        public byte[] Message { get; set; }
        public bool FromMe { get; internal set; }

        public MessageModel()
        {

        }

        public MessageModel(WebMessageInfo info)
        {
            ID = info.Key.Id;
            MessageType = info.MessageStubType.ToString();
            RemoteJid = info.Key.RemoteJid;
            Message = info.ToByteArray();
            FromMe = info.Key.FromMe;
        }

        public string GetID()
        {
            return ID;
        }

        public WebMessageInfo ToMessageInfo()
        {
            return WebMessageInfo.Parser.ParseFrom(Message);
        }
    }

    public class MessageUpdate
    {
        public MessageKey Key { get; set; }
        public MessageUpdateModel Update { get; set; }

        public static MessageUpdate FromRevoke(WebMessageInfo message, Message.Types.ProtocolMessage protocolMsg)
        {
            var result = new MessageUpdate();
            result.Key = message.Key;
            result.Key.Id = protocolMsg.Key.Id;

            result.Update = new MessageUpdateModel();
            result.Update.Message = null;
            result.Update.MessageStubType = message.MessageStubType;
            result.Update.Key = message.Key;

            return result;
        }
    }

    public class MessageUpdateModel
    {
        public Message? Message { get; set; }
        public WebMessageInfo.Types.StubType MessageStubType { get; set; }
        public MessageKey Key { get; set; }
        public bool Starred { get; set; }
    }



    // types to generate WA messages
    /*
    public class Mentionable
    {
        public string[]? Menstions { get; set}
    }

    public class ViewOnce
    {
        public bool? IsViewOnce { get; set; }
    }


    public interface IContextInfo
    {
        string? stanzaId { get; set; }
        string? participant { get; set; }
        IMessage? quotedMessage { get; set; }
        string? remoteJid { get; set; }
        string[]? mentionedJid { get; set; }
        string? conversionSource { get; set; }
        byte[]? conversionData { get; set; }
        int? conversionDelaySeconds { get; set; }
        int? forwardingScore { get; set; }
        bool? isForwarded { get; set; }
        ContextInfo.Types.AdReplyInfo? quotedAd { get; set; }
        MessageKey? placeholderKey { get; set; }
        int? expiration { get; set; }
        long? ephemeralSettingTimestamp { get; set; }
        byte[]? ephemeralSharedSecret { get; set; }
        ContextInfo.Types.ExternalAdReplyInfo? externalAdReply { get; set; }
        string? entryPointConversionSource { get; set; }
        string? entryPointConversionApp { get; set; }
        int? entryPointConversionDelaySeconds { get; set; }
        DisappearingMode? disappearingMode { get; set; }
        ActionLink? actionLink { get; set; }
        string? groupSubject { get; set; }
        string? parentGroupJid { get; set; }
        string? trustBannerType { get; set; }
        int? trustBannerAction { get; set; }
        bool? isSampled { get; set; }
        GroupMention[]? groupMentions { get; set; }
        ContextInfo.Types.UTMInfo? utm { get; set; }
        ContextInfo.Types.ForwardedNewsletterMessageInfo? forwardedNewsletterMessageInfo { get; set; }
        ContextInfo.Types.BusinessMessageForwardInfo? businessMessageForwardInfo { get; set; }
        string? smbClientCampaignId { get; set; }
        string? smbServerCampaignId { get; set; }
        ContextInfo.Types.DataSharingContext? dataSharingContext { get; set; }
    }

    public class Contextable
    {
        public IContextInfo ContextInfo { get; set; }
    }

    public class Buttonable
    {
        public Message.Types.ButtonsMessage.Types.Button[] Buttons { get; set; }
    }

    public class Templatable
    {
        public HydratedTemplateButton[] TemplateButtons { get; set; }
    }

    public class Editable
    {
    }
    public class WAUrlInfo
    {
    }

    public class Listable
    {

    }

    public class AnyMediaMessageContent
    {

    }

    public class RequestPhoneNumber
    {

    }

    public class SharePhoneNumber
    {

    }

    public class WASendableProduct
    {

    }

    public class ButtonReplyInfo
    {

    }

    public class PollMessageOptions
    {

    }

    public class AnyRegularMessageContent
    {
        public string Text { get; set; }
        public WAUrlInfo LinkPreview { get; set; }
        public Mentionable Mentionable { get; set; }
        public Contextable Contextable { get; set; }
        public Buttonable Buttonable { get; set; }
        public Templatable Templatable { get; set; }
        public Listable Listable { get; set; }
        public Editable Editable { get; set; }
        public AnyMediaMessageContent AnyMediaMessageContent { get; set; }
        public PollMessageOptions Poll { get; set; }
        public List<ContactMessage> Contacts { get; set; }
        public LocationMessage Location { get; set; }
        public ReactionMessage Reaction { get; set; }
        public ButtonReplyInfo ButtonReply { get; set; }
        public string Type { get; set; }
        public ListResponseMessage ListReply { get; set; }
        public WASendableProduct Product { get; set; }
        public string BusinessOwnerJid { get; set; }
        public string Body { get; set; }
        public string Footer { get; set; }
        public SharePhoneNumber SharePhoneNumber { get; set; }
        public RequestPhoneNumber RequestPhoneNumber { get; set; }
    }
    */
}
