using Google.Protobuf;
using LiteDB;
using Newtonsoft.Json;
using Proto;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    public enum MessageUpsertType
    {
        Append = 1,
        Notify = 2
    }

    public interface IEventManage
    {
        bool CanMerge(IEventManage eventManage);
        void Merge(IEventManage eventManage);
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

    public class MessageUpsertModel : IEventManage
    {
        public MessageUpsertModel(MessageUpsertType type, params WebMessageInfo[] messages)
        {
            Type = type;
            Messages = messages;
        }

        public MessageUpsertType Type { get; set; }
        public WebMessageInfo[] Messages { get; set; }

        public bool CanMerge(IEventManage eventManage)
        {
            if (eventManage == null)
                return false;
            if (eventManage is MessageUpsertModel newEvent)
            {
                if (newEvent.Type == Type)
                    return true;
                return false;
            }
            return true;
        }

        public void Merge(IEventManage eventManage)
        {
            if (eventManage is MessageUpsertModel newEvent)
            {
                Messages = Messages.Concat(newEvent.Messages).ToArray();
            }
        }
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
}
