using WhatsSocket.Core.Events;

namespace WhatsSocket.Exceptions
{
    [Serializable]
    public class Boom : Exception
    {
        public Boom() { }
        public Boom(string message, DisconnectReason reason) : base(message)
        {
            Reason = reason;
        }

        public DisconnectReason Reason { get; }
    }

}
