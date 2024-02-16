namespace WhatsSocket.Exceptions
{
    [Serializable]
    public class SessionException : Exception
    {
        public SessionException() { }
        public SessionException(string message) : base(message) { }
    }

}
