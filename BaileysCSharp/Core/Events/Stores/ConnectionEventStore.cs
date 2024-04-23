using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Events.Stores
{
    public class ConnectionEventStore : DataEventStore<ConnectionState>
    {
        public ConnectionEventStore() : base(true)
        {
        }

        public event EventHandler<ConnectionState> Update;

        public override void Execute(EmitType value, ConnectionState[] args)
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
