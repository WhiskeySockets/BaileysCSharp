using Google.Protobuf;
using LiteDB;
using Proto;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Types
{
    public enum MessageEventType
    {
        Append = 1,
        Notify = 2,
        Delete = 3,
        Update = 4,
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

    public class MessageEventModel : IEventManage
    {
        public MessageEventModel(MessageEventType type, params WebMessageInfo[] messages)
        {
            Type = type;
            Messages = messages;
        }

        public MessageEventType Type { get; set; }
        public WebMessageInfo[] Messages { get; set; }

        public bool CanMerge(IEventManage eventManage)
        {
            if (eventManage == null)
                return false;
            if (eventManage is MessageEventModel newEvent)
            {
                if (newEvent.Type == Type)
                    return true;
                return false;
            }
            return true;
        }

        public void Merge(IEventManage eventManage)
        {
            if (eventManage is MessageEventModel newEvent)
            {
                Messages = Messages.Concat(newEvent.Messages).ToArray();
            }
        }
    }

    public class MessageReceipt
    {
        public string MessageID { get; set; }
        public string RemoteJid { get; set; }
        public WebMessageInfo.Types.Status Status { get; set; }
        public long Time { get; set; }

        public override string ToString()
        {
            return $"[{DateTimeOffset.FromUnixTimeSeconds(Time).LocalDateTime:yyyy-MM-dd HH:mm}] -> {MessageID} status {Status} for {RemoteJid}";
        }
    }

    public class MessageModel : IMayHaveID
    {
        [BsonId]
        public string ID { get; set; }

        public string MessageType { get; set; }
        public string RemoteJid { get; set; }
        public bool FromMe { get; internal set; }

        //THIS IS A VERY BAD APPROACH, TEMPORARY ONLY, I WAS LAZY
        public byte[] Message { get; set; }

        public DateTime MessageDate { get; set; }

        public MessageModel()
        {
        }

        public MessageModel(WebMessageInfo info)
        {
            ID = info.Key.Id;
            MessageType = info.MessageStubType.ToString();
            RemoteJid = info.Key.RemoteJid;
            FromMe = info.Key.FromMe;
           
            try
            {
                var protocolMsg = info.Message?.ProtocolMessage ?? null;
                var messageTimestamp = protocolMsg != null && protocolMsg.TimestampMs != 0
                                  ? Math.Floor((protocolMsg.TimestampMs / 1000M))
                                  : info.MessageTimestamp;
                MessageDate = DateTimeOffset.FromUnixTimeSeconds((long)info.MessageTimestamp).LocalDateTime;
            }
            catch (Exception e)
            {
                if (info.MessageTimestamp > 253402300799)
                {
                    MessageDate = DateTimeOffset.FromUnixTimeMilliseconds((long)info.MessageTimestamp).LocalDateTime;
                }
                else
                {
                    MessageDate = DateTimeOffset.FromUnixTimeSeconds((long)info.MessageTimestamp).LocalDateTime;
                }
            }


            //THIS IS A VERY BAD APPROACH, TEMPORARY ONLY, I WAS LAZY
            Message = info.ToByteArray();
        }

        public string GetID()
        {
            return ID;
        }

        //THIS IS A VERY BAD APPROACH, TEMPORARY ONLY, I WAS LAZY
        public WebMessageInfo ToMessageInfo()
        {
            return WebMessageInfo.Parser.ParseFrom(Message);
        }


        public List<MessageReceipt> Receipts { get; set; }

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
        public WebMessageInfo.Types.StubType? MessageStubType { get; set; }
        public WebMessageInfo.Types.Status? Status { get; set; }
        public MessageKey Key { get; set; }
        public bool? Starred { get; set; }
    }
}
