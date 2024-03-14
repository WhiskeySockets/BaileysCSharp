namespace WhatsSocket.Core.Models
{
    public class RetryMedia
    {
        public byte[] IV { get; set; }
        public byte[] CipherText { get; set; }
    }

}