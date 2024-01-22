namespace WhatsSocket.Core.Models
{
    public class PreKeySet
    {
        public Dictionary<int, KeyPair> NewPreKeys { get; set; }
        public int LastPreKeyId { get; set; }
        public int[] PreKeyRange { get; set; }
    }
}
