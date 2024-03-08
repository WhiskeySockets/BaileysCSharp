using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Stores;
using WhatsSocket.Exceptions;

namespace WhatsSocket.Core.Signal
{
    internal class GroupCipher
    {
        public GroupCipher(SessionStore storage, string senderName)
        {
            Storage = storage;
            SenderName = senderName;
        }

        public SessionStore Storage { get; }
        public string SenderName { get; }

        public byte[] Decrypt(byte[] data)
        {
            var record = Storage.LoadSenderKey(SenderName);

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


            Storage.StoreSenderKey(SenderName, record);

            return plainText;
        }

        private byte[] GetPlainText(byte[] iv, byte[] cipherKey, byte[] ciphertext)
        {
            return EncryptionHelper.DecryptAesCbcWithIV(ciphertext, cipherKey, iv);
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
