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
        public Boom(string message, object data) : base(message)
        {
            Data = data;
        }
        public Boom(string message) : base(message)
        {
            Reason = DisconnectReason.None;
        }

        public DisconnectReason Reason { get; }
        public object Data { get; }
    }

}
