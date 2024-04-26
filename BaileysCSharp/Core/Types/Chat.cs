using LiteDB;
using Proto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.NoSQL;
using static BaileysCSharp.Core.Utils.GenericUtils;
using BaileysCSharp.Core.Utils;
using BaileysCSharp.Core.WABinary;

namespace BaileysCSharp.Core.Models
{


    public enum WAPrivacyValue
    {
        All,
        Contacts,
        Contact_Blacklist,
        None
    }

    public enum WAPrivacyOnlineValue
    {
        All,
        Match_Last_Seen
    }

    public enum WAReadReceiptsValue
    {
        All,
        None
    }

    public enum WAPresence
    {
        Unavailable,
        Available,
        Composing,
        Recording,
        Paused

    }

    public class StatusResult
    {
        public string Status { get; set; }
        public ulong SetAt { get; set; }
    }

    public class OnWhatsAppResult
    {
        public bool Exists { get; set; }
        public string Jid { get; set; }

        public OnWhatsAppResult(BinaryNode node)
        {
            var contact = GetBinaryNodeChild(node, "contact");
            Exists = contact?.getattr("type") == "ind";
            Jid = node.getattr("jid") ?? "";
        }
    }

    public class PresenceData
    {
        public WAPresence LastKnownPresence { get; set; }
        public ulong? LastSeen { get; set; }
    }

    public class PresenceModel
    {
        public string ID { get; set; }
        public PresenceModel()
        {
            Presences = new Dictionary<string, PresenceData>();
        }

        public Dictionary<string, PresenceData> Presences { get; set; }

        internal static WAPresence Map(string tag)
        {
            var dict = new Dictionary<string, WAPresence>()
            {
                {"unavailable", WAPresence.Unavailable },
                {"available", WAPresence.Available },
                {"composing", WAPresence.Composing },
                {"recording", WAPresence.Recording },
                {"paused", WAPresence.Paused },
            };
            return dict[tag];
        }
    }

    public class ChatConstants
    {
        public static string[] ALL_WA_PATCH_NAMES = [WAPatchName.CriticalBlock, WAPatchName.CriticalUnblockLow, WAPatchName.RegularHigh, WAPatchName.RegularLow, WAPatchName.Regular];
    }

    public class WAPatchName
    {
        public const string CriticalBlock = "critical_block";
        public const string CriticalUnblockLow = "critical_unblock_low";
        public const string RegularHigh = "regular_high";
        public const string RegularLow = "regular_low";
        public const string Regular = "regular";
    }

    public class ChatModel : IMayHaveID
    {
        public ChatModel()
        {

        }

        [BsonId]
        public string ID { get; set; }
        public ulong ConversationTimestamp { get; set; }
        public ulong LastMessageRecvTimestamp { get; set; }
        public ulong UnreadCount { get; set; }
        public bool ReadOnly { get; set; }
        public bool Archived { get; set; }

        public ulong EphemeralSettingTimestamp { get; set; }
        public ulong EphemeralExpiration { get; set; }
        public string Name { get; set; }

        public byte[] TcToken { get; set; }
        public long MuteEndTime { get; set; }
        public long Pinned { get; internal set; }

        public bool IsGroup
        {
            get
            {
                return JidUtils.IsJidGroup(ID);
            }
        }

        public string GetID()
        {
            return ID;
        }

        public override string ToString()
        {
            return $"{ID} - {Name}";
        }
    }


    public class ChatMutation
    {
        public SyncActionData SyncAction { get; set; }
        public string[] Index { get; set; }
    }

    public class ChatMutationMap : IEnumerable<string>
    {
        private List<KeyValuePair<string, ChatMutation>> Items { get; set; }
        private Dictionary<string, int> AddCount { get; set; }
        private Dictionary<string, int> GetCount { get; set; }

        public ChatMutationMap()
        {
            Items = new List<KeyValuePair<string, ChatMutation>>();
            AddCount = new Dictionary<string, int>();
            GetCount = new Dictionary<string, int>();
        }

        public IEnumerator<string> GetEnumerator()
        {
            var items = Items.Select(x => x.Key).ToList();

            return items.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Assign(ChatMutationMap mutationMap)
        {
            foreach (var item in mutationMap)
            {
                this[item] = mutationMap[item];
            }
        }

        public ChatMutation this[string key]
        {
            get
            {
                if (GetCount.ContainsKey(key))
                {
                    GetCount[key] = GetCount[key] + 1;
                }
                else
                {
                    GetCount[key] = 0;
                }
                var matching = Items.Where(x => x.Key == key);
                return matching.Skip(GetCount[key]).FirstOrDefault().Value;
            }
            set
            {
                if (AddCount.ContainsKey(key))
                {
                    AddCount[key] = AddCount[key] + 1;
                }
                else
                {
                    AddCount[key] = 1;
                }
                Items.Add(new KeyValuePair<string, ChatMutation>(key, value));
            }
        }
    }

    public abstract class ChatModification
    {

    }

    public class MinimalMessage
    {
        public MessageKey Key { get; set; }
        public long MessageTimestamp { get; set; }
    }

    public class ArchiveChatModification : ChatModification
    {
        public bool Archive { get; set; }
        public List<MinimalMessage> LastMessages { get; set; }
    }
    public class MuteChatModification : ChatModification
    {
        public long? Mute { get; set; }
    }

    public class PushNameChatModification : ChatModification
    {
        public string PushNameSetting { get; set; }
    }

    public class PinChatModification : ChatModification
    {
        public bool Pin { get; set; }
    }

    //TODO
    public class ClearChatModification : ChatModification
    {

    }


    public class StarMessage
    {
        public string ID { get; set; }
        public bool FromMe { get; set; }
    }

    public class StarChatModification : ChatModification
    {
        public List<StarMessage> Messages { get; set; }
        public bool Star { get; set; }
    }

    public class MarkReadChatModification : ChatModification
    {
        public bool MarkRead { get; set; }
        public List<MinimalMessage> LastMessages { get; set; }
    }

    public class DeleteChatModification : ChatModification
    {
        public bool Delete { get; set; }
        public List<MinimalMessage> LastMessages { get; set; }
    }

    public class ChatLabelAssociationActionBody
    {
        public string LabelID { get; set; }
    }
    public class MessageLabelAssociationActionBody
    {
        public string MessageID { get; set; }
        public string LabelID { get; set; }
    }


    public class AddChatLableChatModification : ChatModification
    {
        public ChatLabelAssociationActionBody AddChatLabel { get; set; }
    }

    public class RemoveChatLableChatModification : ChatModification
    {
        public ChatLabelAssociationActionBody RemoveChatLabel { get; set; }
    }

    public class AddMessageLabelChatModification : ChatModification
    {
        public MessageLabelAssociationActionBody AddMessageLabel { get; set; }
    }
    public class RemoveMessageLabelChatModification : ChatModification
    {
        public MessageLabelAssociationActionBody RemoveMessageLabel { get; set; }
    }

    public class WAPatchCreate
    {
        public SyncActionValue SyncAction { get; set; }
        public string[] Index { get; set; }
    }
}
