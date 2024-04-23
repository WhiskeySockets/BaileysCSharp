namespace BaileysCSharp.Core.Models
{
    public class EncryptData
    {
        public int Type { get; set; }
        public byte[] Data { get; set; }
        public ulong RegistrationId { get; set; }
    }


    public class CipherMessage
    {
        public string Type { get; set; }
        public byte[] CipherText { get; set; }
    }

}
