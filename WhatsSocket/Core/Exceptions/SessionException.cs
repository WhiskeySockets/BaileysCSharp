namespace WhatsSocket.Exceptions
{
    [Serializable]
    public class SessionException : Exception
    {
        public SessionException() { }
        public SessionException(string message) : base(message) { }
    }


    [Serializable]
    public class MessageCounterError : Exception
    {
        public MessageCounterError() { }
        public MessageCounterError(string message) : base(message) { }
    }
}
