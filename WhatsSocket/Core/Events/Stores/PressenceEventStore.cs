using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Events.Stores
{
    public class PressenceEventStore : DataEventStore<PresenceModel>
    {
        public PressenceEventStore() : base(false)
        {
        }

        public event EventHandler<PresenceModel> Update;

        public override void Execute(EmitType value, PresenceModel[] args)
        {
            foreach (var item in args)
            {
                switch (value)
                {
                    case EmitType.Update:
                        Update?.Invoke(this, item);
                        break;
                }
            }
        }
    }

}
