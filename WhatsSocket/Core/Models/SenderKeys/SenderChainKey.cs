using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Models.SenderKeys
{
    public class SenderChainKey
    {
        static byte[] MESSAGE_KEY_SEED = [0x01];
        static byte[] CHAIN_KEY_SEED = [0x02];

        public SenderChainKey(uint iteration, byte[] chainKey)
        {
            Iteration = iteration;
            ChainKey = chainKey;
        }

        public uint Iteration { get; }
        public byte[] ChainKey { get; }

        internal SenderChainKey GetNext()
        {
            var derivative = CryptoUtils.GetDerivative(CHAIN_KEY_SEED, ChainKey);
            return new SenderChainKey(Iteration + 1, derivative);
        }
        internal SenderMessageKey GetSenderMessageKey()
        {
            var derivative = CryptoUtils.GetDerivative(MESSAGE_KEY_SEED, ChainKey);
            return new SenderMessageKey(Iteration, derivative);
        }
    }


}
