using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Events.Stores
{
    public class MessageReceiptEventStore : DataEventStore<MessageReceipt>
    {
        public MessageReceiptEventStore() : base(false)
        {
        }

        public event EventHandler<MessageReceipt[]> Upsert;
        public event EventHandler<MessageReceipt[]> Update;

        public override void Execute(EmitType value, MessageReceipt[] args)
        {
            switch (value)
            {
                //Internal Use
                case EmitType.Upsert:
                    Upsert?.Invoke(this, args);
                    break;

                //Notify
                case EmitType.Update:
                    Update?.Invoke(this, args);
                    break;
            }
        }
    }

}
