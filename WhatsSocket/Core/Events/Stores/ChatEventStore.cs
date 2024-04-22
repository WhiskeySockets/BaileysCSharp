using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Events.Stores
{
    public class ChatEventStore : DataEventStore<ChatModel>
    {
        public ChatEventStore() : base(true)
        {
        }

        public event EventHandler<ChatModel[]> Upsert;
        public event EventHandler<ChatModel[]> Update;
        public event EventHandler<ChatModel[]> Delete;

        public override void Execute(EmitType value, ChatModel[] args)
        {
            switch (value)
            {
                case EmitType.Upsert:
                    Upsert?.Invoke(this, args);
                    break;
                case EmitType.Update:
                    Update?.Invoke(this, args);
                    break;
                case EmitType.Delete:
                    Delete?.Invoke(this, args);
                    break;
            }
        }
    }

}
