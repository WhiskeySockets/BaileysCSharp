using System.Text;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Models.SenderKeys
{
    public class SenderMessageKey
    {
        public uint Iteration { get; set; }
        public byte[] Seed { get; set; }

        public byte[] CipherKey { get; set; }
        public byte[] IV { get; set; }

        public SenderMessageKey(uint iteration, byte[] seed)
        {
            var derivative = EncryptionHelper.DeriveSecrets(seed, new byte[32], Encoding.UTF8.GetBytes("WhisperGroup"));
            this.CipherKey = new byte[32];
            CipherKey.Set(derivative[0].Slice(16));
            CipherKey.Set(derivative[1].Slice(0, 16), 16);
            IV = derivative[0].Slice(0, 16);
            Iteration = iteration;
            Seed = seed;
        }
    }


}
