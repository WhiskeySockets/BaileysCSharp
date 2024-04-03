using Google.Protobuf;

namespace WhatsSocket.LibSignal
{
    [Serializable]
    public class SessonException : Exception
    {
        public SessonException() { }
        public SessonException(string message) : base(message) { }
        public SessonException(string message, Exception inner) : base(message, inner) { }
        protected SessonException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UntrustedIdentityKeyError : Exception
    {
        public UntrustedIdentityKeyError() { }
        public UntrustedIdentityKeyError(string message) : base(message) { }

        public UntrustedIdentityKeyError(string address, ByteString identityKey) : this($"{address} is not trused")
        {
            Address = address;
            IdentityKey = identityKey;
        }

        public string Address { get; }
        public ByteString IdentityKey { get; }
    }


}
