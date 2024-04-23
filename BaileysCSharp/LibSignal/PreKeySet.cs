using BaileysCSharp.LibSignal;

namespace BaileysCSharp.Core.Models
{
    public class PreKeySet
    {
        public Dictionary<uint, KeyPair> NewPreKeys { get; set; }
        public uint LastPreKeyId { get; set; }
        public uint[] PreKeyRange { get; set; }
    }
}
