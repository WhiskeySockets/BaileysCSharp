using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement.Kdf;
using Proto;
using System;
using System.Buffers;
using System.Text;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Helper;
using Google.Protobuf;
using WhatsSocket.Core.Credentials;

namespace WhatsSocket.Core
{
    internal class NoiseHandler
    {

        public event EventHandler<BinaryNode> OnFrame;

        public NoiseHandler(KeyPair ephemeralKeyPair, Logger logger) 
        {
            EphemeralKeyPair = ephemeralKeyPair;
            Logger = logger;
            Initialize();
        }

        public byte[] InBytes = new byte[0];
        public bool IsFinished { get; set; }
        public byte[] Hash { get; set; }
        public byte[] EncKey { get; set; }
        public byte[] DecKey { get; set; }
        public byte[] Salt { get; set; }

        uint readCounter;
        uint writeCounter;
        public bool SetIntro { get; set; }
        bool IsMobile { get; set; }
        public KeyPair EphemeralKeyPair { get; }
        public Logger Logger { get; }

        private void Initialize()
        {
            byte[] data = Encoding.UTF8.GetBytes(Constants.NoiseMode);

            Hash = data.Length == 32 ? data : EncryptionHelper.Sha256(data);
            Salt = Hash;
            EncKey = Hash;
            DecKey = Hash;
            readCounter = 0;
            writeCounter = 0;
            IsFinished = false;

            Authenticate(Constants.NOISE_HEADER);
            Authenticate(EphemeralKeyPair.Public);
        }

        public byte[] Encrypt(byte[] plaintext)
        {
            var result = EncryptionHelper.EncryptAESGCM(plaintext, EncKey, GenerateIV(writeCounter), Hash);
            writeCounter++;
            Authenticate(result);
            return result;
        }


        private void Authenticate(ByteString auth)
        {
            Authenticate(auth.ToByteArray());
        }

        private void Authenticate(byte[] buffer)
        {
            if (!IsFinished)
            {
                var concattted = Hash.Concat(buffer);
                Hash = EncryptionHelper.Sha256(Hash.Concat(buffer));
            }
        }

        private byte[] Decrypt(ByteString ciphertext)
        {
            return Decrypt(ciphertext.ToByteArray());
        }

        private byte[] Decrypt(byte[] ciphertext)
        {
            var iv = GenerateIV(IsFinished ? readCounter : writeCounter);
            var result = EncryptionHelper.DecryptAESGCM(ciphertext, DecKey, iv, Hash);

            if (IsFinished)
            {
                readCounter++;
            }
            else
            {
                writeCounter++;
            }
            Authenticate(ciphertext);

            return result;
        }

        int salted = 0;

        private void MixIntoKey(byte[] bytes)
        {
            var writeRead = LocalHKDF(bytes);
            Salt = writeRead.write;
            EncKey = writeRead.read;
            DecKey = writeRead.read;
            readCounter = 0;
            writeCounter = 0;
        }
        public void FinishInit()
        {
            var writeRead = LocalHKDF(new byte[0]);
            EncKey = writeRead.read;
            DecKey = writeRead.read;
            Hash = new byte[0];
            readCounter = 0;
            writeCounter = 0;
            IsFinished = true;
        }

        public byte[] EncodeFrame(byte[] data)
        {
            if (IsFinished)
            {
                data = Encrypt(data);
            }

            var introSize = SetIntro ? 0 : Constants.NOISE_HEADER.Length;
            byte[] buffer = new byte[introSize + 3 + data.Length];
            if (!SetIntro)
            {
                Constants.NOISE_HEADER.CopyTo(buffer, 0);
                SetIntro = true;
            }

            var databyteLength = (data.Length >> 16).UInt8ToBigEndianBytes();
            databyteLength.CopyTo(buffer, introSize);
            databyteLength = (65535 & data.Length).UInt16ToBigEndianBytes();
            databyteLength.CopyTo(buffer, introSize + 1);

            data.CopyTo(buffer, introSize + 3);

            return buffer;

        }
        public byte[] ProcessHandShake(HandshakeMessage result, KeyPair noiseKey)
        {
            Authenticate(result.ServerHello.Ephemeral);
            MixIntoKey(EncryptionHelper.SharedKey(EphemeralKeyPair.Private, result.ServerHello.Ephemeral));

            var decStaticContent = Decrypt(result.ServerHello.Static);
            MixIntoKey(EncryptionHelper.SharedKey(EphemeralKeyPair.Private, decStaticContent));

            var certDecoded = Decrypt(result.ServerHello.Payload);

            if (IsMobile)
            {

            }
            else
            {
                var certIntermediate = CertChain.Parser.ParseFrom(certDecoded);
                var issuerSerial = CertChain.Types.NoiseCertificate.Types.Details.Parser.ParseFrom(certIntermediate.Intermediate.Details);
                if (issuerSerial.IssuerSerial != Constants.WA_CERT_DETAILS_SERIAL)
                {
                    throw new Exception("certification match failed");
                }
            }

            var keyEnc = Encrypt(noiseKey.Public);
            MixIntoKey(EncryptionHelper.SharedKey(noiseKey.Private.ToByteString(), result.ServerHello.Ephemeral));

            return keyEnc;
        }


        private byte[] GenerateIV(uint writeCounter)
        {
            byte[] iv = new byte[12];
            var converted = BitConverter.GetBytes(writeCounter);
            converted = converted.Reverse().ToArray();
            converted.CopyTo(iv, 8);
            return iv;
        }


        public void DecodeFrame(byte[] newData, Action<BinaryNode> action)
        {
            // the binary protocol uses its own framing mechanism
            // on top of the WS frames
            // so we get this data and separate out the frames
            Func<int> getBytesSize = () =>
            {
                if (InBytes.Length >= 3)
                {
                    var sizeBuffer = InBytes.Skip(1).Take(2).Reverse().ToArray();
                    return (InBytes[0] >> 16) | BitConverter.ToUInt16(sizeBuffer);
                }
                return 0;
            };

            InBytes = InBytes.Concat(newData).ToArray();

            Logger.Trace($"recv {newData.Length} bytes, total recv {InBytes.Length} bytes");


            var size = getBytesSize();
            while (size > 0 && InBytes.Length >= size + 3)
            {
                var frame = InBytes.Skip(3).Take(size).ToArray();
                InBytes = InBytes.Skip(3 + size).ToArray();

                var message = new BinaryNode()
                {
                    tag = "handshake",
                    attrs = new Dictionary<string, string>(),
                    content = frame,
                };

                if (IsFinished)
                {
                    var decrypted = Decrypt(message.ToByteArray());
                    message = BufferDecoder.DecodeBinaryNode(decrypted);
                }

                if (message.attrs.ContainsKey("id"))
                {
                    Logger.Trace(new { msg = message.attrs["id"] }, "recv frame");
                }
                else
                {
                    Logger.Trace("recv frame");
                }

                action(message);
                size = getBytesSize();
            }
        }



        public (byte[] write, byte[] read) LocalHKDF(byte[] bytes)
        {
            var hkdf = EncryptionHelper.HKDF(bytes, 64, Salt, Encoding.UTF8.GetBytes(""));
            return (hkdf.Take(32).ToArray(), hkdf.Skip(32).ToArray());
        }


    }
}
