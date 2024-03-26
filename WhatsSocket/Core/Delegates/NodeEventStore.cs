using System.Diagnostics;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Delegates
{
    public class NodeEventStore 
    {
        public event EventEmitterHandler<BinaryNode> Emit;
        public BaseSocket Sender { get; }

        private NodeEventStore()
        {
        }

        public NodeEventStore(BaseSocket sender)
        {
            Sender = sender;
        }

        public bool Execute(BinaryNode args)
        {
            if (args != null)
            {
                if (Emit != null)
                {
                    Emit.Invoke(Sender, args);
                    return true;
                }
            }
            return false;
        }

    }
}
