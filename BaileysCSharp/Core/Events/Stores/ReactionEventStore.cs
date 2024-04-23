using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Events.Stores
{
    public class ReactionEventStore : DataEventStore<MessageReactionModel>
    {
        public ReactionEventStore() : base(false)
        {
        }

        public event EventHandler<MessageReactionModel> React;

        public override void Execute(EmitType value, MessageReactionModel[] args)
        {
            foreach (var item in args)
            {
                switch (value)
                {
                    case EmitType.Reaction:
                        React?.Invoke(this, item);
                        break;
                }
            }
        }
    }

}
