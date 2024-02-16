namespace WhatsSocket.Core.Models
{
    public class CipherTextMessage
    {
        public const int UNSUPPORTED_VERSION = 1;
        public const int CURRENT_VERSION = 3;
        public const int WHISPER_TYPE = 2;
        public const int PREKEY_TYPE = 3;
        public const int SENDERKEY_TYPE = 4;
        public const int SENDERKEY_DISTRIBUTION_TYPE = 5;
        public const int ENCRYPTED_MESSAGE_OVERHEAD = 53;
    }
}
