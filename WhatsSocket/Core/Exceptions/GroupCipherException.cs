namespace WhatsSocket.Exceptions
{
    [Serializable]
    public class GroupCipherException : Exception
    {
        public GroupCipherException() { }
        public GroupCipherException(string message) : base(message) { }
    }

}
