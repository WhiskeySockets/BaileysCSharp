using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Events.Stores
{
    public class MessageHistoryEventStore : DataEventStore<MessageHistoryModel>
    {

        public MessageHistoryEventStore() : base(true)
        {
        }

        public event EventHandler<MessageHistoryModel[]> Set;

        public override void Execute(EmitType value, MessageHistoryModel[] args)
        {
            switch (value)
            {
                case EmitType.Set:
                    Set?.Invoke(this, args);
                    break;
            }
        }
    }

}
