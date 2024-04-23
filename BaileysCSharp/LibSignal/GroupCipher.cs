using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Signal;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Exceptions;

namespace BaileysCSharp.LibSignal
{
    internal class GroupCipher
    {
        public GroupCipher(SignalStorage store, string senderName)
        {
            Store = store;
            SenderName = senderName;
        }

        public SignalStorage Store { get; }
        public string SenderName { get; }

        public byte[] Decrypt(byte[] data)
        {
            var record = Store.LoadSenderKey(SenderName);

            if (record == null)
            {
                throw new GroupCipherException("No SenderKeyRecord found for decryption");
            }
            var senderKeyMessage = new SenderKeyMessage(data);
            var senderKeyState = record.GetSenderStateKey(senderKeyMessage.KeyId);
            if (senderKeyState == null)
            {
                throw new GroupCipherException("No session found to decrypt message");
            }

            senderKeyMessage.VerifySignature(senderKeyState.SenderSigningKey.Public);

            var senderKey = GetSenderKey(senderKeyState, senderKeyMessage.Iteration);


            var plainText = GetPlainText(senderKey.IV, senderKey.CipherKey, senderKeyMessage.Ciphertext);


            Store.StoreSenderKey(SenderName, record);

            return plainText;
        }

        internal byte[] Encrypt(byte[] paddedPlaintext)
        {
            var record = Store.LoadSenderKey(SenderName);

            if (record == null)
            {
                throw new GroupCipherException("No SenderKeyRecord found for Encryption");
            }

            var senderKeyState = record.GetSenderStateKey(0);
            if (senderKeyState == null)
            {
                throw new GroupCipherException("No session found to encrypt message");
            }

            var iteration = senderKeyState.GetSenderChainKey().Iteration;
            var senderKey = GetSenderKey(senderKeyState, iteration == 0 ? 0 : iteration + 1);

            var cipherText = CryptoUtils.EncryptAesCbc(paddedPlaintext, senderKey.CipherKey, senderKey.IV);

            var senderKeyMessage = new SenderKeyMessage(senderKeyState.SenderKeyId, senderKey.Iteration, cipherText, senderKeyState.GetSigningKeyPrivate());

            Store.StoreSenderKey(SenderName, record);

            return senderKeyMessage.Serialize();
        }

        private byte[] GetPlainText(byte[] iv, byte[] cipherKey, byte[] ciphertext)
        {
            return CryptoUtils.DecryptAesCbcWithIV(ciphertext, cipherKey, iv);
        }


        //Todo Finalize this
        private SenderMessageKey GetSenderKey(SenderKeyState senderKeyState, uint iteration)
        {
            var senderChainKey = senderKeyState.GetSenderChainKey();
            if (senderChainKey.Iteration > iteration)
            {
                if (senderKeyState.HasSenderMessageKey(iteration))
                {
                    return senderKeyState.RemoveSenderMessageKey(iteration);
                }
                throw new GroupCipherException($"Received message with old counter:");
            }

            if (iteration - senderChainKey.Iteration > 2000)
            {
                throw new GroupCipherException($"Over 2000 messages into the future!");
            }
            while (senderChainKey.Iteration < iteration)
            {
                senderKeyState.AddSenderMessageKey(senderChainKey.GetSenderMessageKey());
                senderChainKey = senderChainKey.GetNext();
            }
            senderKeyState.SetSenderChainKey(senderChainKey.GetNext());
            return senderChainKey.GetSenderMessageKey();
        }
    }
}
