﻿namespace WhatsSocket.Core.Models
{
    public class ChatModel
    {
        public ChatModel()
        {
        }

        public string ID { get; set; }
        public ulong ConversationTimestamp { get; set; }
        public ulong LastMessageRecvTimestamp { get; set; }
        public ulong UnreadCount { get; set; }
        public bool ReadOnly { get; internal set; }
        public bool Archived { get; internal set; }

        public ulong EphemeralSettingTimestamp { get; set; }
        public ulong EphemeralExpiration { get; set; }
        public string Name { get; internal set; }
    }

}