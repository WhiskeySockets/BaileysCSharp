using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Events.Stores
{
    public class AuthEventStore : DataEventStore<AuthenticationCreds>
    {
        public AuthEventStore() : base(false)
        {
        }

        public event EventHandler<AuthenticationCreds> Update;

        public override void Execute(EmitType value, AuthenticationCreds[] args)
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
