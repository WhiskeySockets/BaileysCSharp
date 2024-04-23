using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Exceptions;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.LibSignal
{

    public class SenderKeyMessage : CipherTextMessage
    {
        public const int SIGNATURE_LENGTH = 64;
        public SenderKeyMessage(uint keyId, uint iteration, byte[] ciphertext, byte[] signatureKey)
        {
            KeyId = keyId;
            Iteration = iteration;
            Ciphertext = ciphertext;
            SignatureKey = signatureKey;



            int version = ((CURRENT_VERSION << 4 | CURRENT_VERSION) & 0xff) % 256;

            var message = new Proto.SenderKeyMessage()
            {
                Id = keyId,
                Ciphertext = ciphertext.ToByteString(),
                Iteration = iteration
            }.ToByteArray();

            var signature = GetSignature(signatureKey, new byte[] { (byte)version }.Concat(message).ToArray());
            _serialized = new byte[] { (byte)version }.Concat(message).Concat(signature).ToArray();
        }

        public SenderKeyMessage(byte[] serialized)
        {
            var version = serialized[0];
            byte[] message = serialized.Skip(1).Take(serialized.Length - SIGNATURE_LENGTH - 1).ToArray();
            byte[] signature = serialized.Skip(1 + message.Length).ToArray();


            var senderKeyMessage = Proto.SenderKeyMessage.Parser.ParseFrom(message);
            MessageVersion = (version & 0xff) >> 4;
            KeyId = senderKeyMessage.Id;
            Iteration = senderKeyMessage.Iteration;
            Ciphertext = senderKeyMessage.Ciphertext.ToByteArray();
            SignatureKey = signature;
            _serialized = serialized;

        }

        private byte[] _serialized;

        public int MessageVersion { get; set; }
        public uint KeyId { get; }
        public uint Iteration { get; }
        public byte[] Ciphertext { get; }
        public byte[] SignatureKey { get; }

        internal void VerifySignature(byte[] signatureKey)
        {
            byte[] part1 = _serialized.Take(_serialized.Length - SIGNATURE_LENGTH).ToArray();
            byte[] part2 = _serialized.Skip(part1.Length).ToArray();
            var valid = Curve.Verify(signatureKey.Skip(1).ToArray(), part1, part2);
            if (!valid)
            {
                throw new GroupCipherException("Invalid Signature");
            }
        }

        public byte[] GetSignature(byte[] signatureKey, byte[] serialized)
        {
            return CryptoUtils.Sign(signatureKey, serialized);
        }

        internal byte[] Serialize()
        {
            return _serialized;
        }
    }
}
