using LiteDB;
using Proto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    public enum WAPresence 
    {
        Unavailable,
        Available,
        Composing,
        Recording,
        Paused

    }

    public class ChatConstants
    {
        public static string[] ALL_WA_PATCH_NAMES = ["critical_block", "critical_unblock_low", "regular_high", "regular_low", "regular"];
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
}
