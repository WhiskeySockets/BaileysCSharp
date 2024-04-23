using Proto;
using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Events.Stores
{
    public class MessagingEventStore : DataEventStore<MessageEventModel>
    {
        public MessagingEventStore() : base(true)
        {
        }

        public event EventHandler<MessageEventModel> Upsert;
        public event EventHandler<WebMessageInfo[]> Update;
        public event EventHandler<WebMessageInfo[]> Delete;

        public override void Execute(EmitType value, MessageEventModel[] args)
        {
            foreach (var item in args)
            {
                switch (value)
                {
                    case EmitType.Upsert:
                        Upsert?.Invoke(this, item);
                        break;

                    case EmitType.Update:
                        Update?.Invoke(this, item.Messages);
                        break;

                    case EmitType.Delete:
                        Delete?.Invoke(this, item.Messages);
                        break;

                }
            }
        }
    }

}
