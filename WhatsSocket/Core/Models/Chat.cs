namespace WhatsSocket.Core.Models
{
    internal class Chat
    {
        public Chat()
        {
        }

        public string ID { get; set; }
        public ulong ConversationTimestamp { get; set; }
        public ulong UnreadCount { get;  set; }
        public bool ReadOnly { get; internal set; }
        public bool Archived { get; internal set; }
    }
}