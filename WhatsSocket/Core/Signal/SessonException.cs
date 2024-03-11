namespace WhatsSocket.Core.Signal
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
}
