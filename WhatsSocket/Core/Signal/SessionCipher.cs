using Google.Protobuf;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Textsecure;
using WhatsSocket.Core.Curve;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.Stores;
using WhatsSocket.Exceptions;

namespace WhatsSocket.Core.Signal
{
    public class SessionCipher
    {
        public SessionCipher(SessionStore storage, ProtocolAddress address)
        {
            Storage = storage;
            Address = address;
        }

        public SessionStore Storage { get; }
        public ProtocolAddress Address { get; }


        public SessionRecord? GetRecord()
        {
            return Storage.Get(Address.ToString());
        }

        internal byte[] DecryptPreKeyWhisperMessage(byte[] data)
        {
            var versions = decodeTupleByte(data[0]);
            if (versions[1] > 3 || versions[0] < 3)
            {  // min version > 3 or max version < 3
                throw new SessionException("Incompatible version number on PreKeyWhisperMessage");
            }

            var record = GetRecord();
            if (record == null)
            {
                record = new SessionRecord();
            }

            var preKeyProto = PreKeyWhisperMessage.Parser.ParseFrom(data.Skip(1).ToArray());

            var builder = new SessionBuilder(Storage, Address);
            var preKeyId = builder.InitIncoming(record, preKeyProto);
            var session = record.getSession(preKeyProto.BaseKey.ToBase64());
            var plaintext = DoDecryptWhisperMessage(preKeyProto.Message.ToByteArray(), session);
            StoreRecord(record);
            if (preKeyId > 0)
            {
                Storage.RemovePreKey(preKeyId);
            }
            return plaintext;
        }

        private void StoreRecord(SessionRecord record)
        {
            record.RemoveOldSessions();
            Storage.Set(Address.ToString(), record);
        }

        private byte[] DoDecryptWhisperMessage(byte[] messageBuffer, Session? session)
        {
            var versions = decodeTupleByte(messageBuffer[0]);

            var messageProto = messageBuffer.Skip(1).Take(messageBuffer.Length - 1 - 8).ToArray();
            var message = WhisperMessage.Parser.ParseFrom(messageProto);

            MaybeStepRatchet(session, message.EphemeralKey, message.PreviousCounter);
            var chain = session.GetChain(message.EphemeralKey.ToByteArray());
            if (chain.ChainType == ChainType.SENDING)
            {
                throw new SessionException("Tried to decrypt on a sending chain");
            }
            FillMessageKeys(chain, message.Counter);
            if (!chain.MessageKeys.ContainsKey((int)message.Counter))
            {
                throw new SessionException("Key used already or never filled");
            }
            var messageKey = chain.MessageKeys[(int)message.Counter];
            chain.MessageKeys.Remove((int)message.Counter);
            var keys = EncryptionHelper.DeriveSecrets(messageKey, new byte[32], Encoding.UTF8.GetBytes("WhisperMessageKeys"));


            var ourIdentityKey = Storage.GetOurIdentity();
            var macInput = new byte[messageProto.Length + 33 * 2 + 1];
            macInput.Set(session.IndexInfo.RemoteIdentityKey);
            macInput.Set(ourIdentityKey.Public, 33);
            macInput[33 * 2] = _encodeTupleByte(3, 3);
            macInput.Set(messageProto, 33 * 2 + 1);

            EncryptionHelper.VerifyMac(macInput, keys[1], messageBuffer.Skip(messageBuffer.Length - 8).ToArray(), 8);

            var plaintext = EncryptionHelper.DecryptAesCbc(message.Ciphertext.ToByteArray(), keys[0], keys[2].Take(16).ToArray());
            session.PendingPreKey = null;
            return plaintext;
        }

        private byte _encodeTupleByte(int number1, int number2)
        {
            if (number1 > 15 || number2 > 15)
            {
                throw new SessionException("Numbers must be 4 bits or less");
            }
            return (byte)(number1 << 4 | number2);
        }

        private void MaybeStepRatchet(Session? session, ByteString remoteKey, uint previousCounter)
        {
            if (session.GetChain(remoteKey.ToByteArray()) != null)
            {
                return;
            }
            var ratchet = session.CurrentRatchet;
            var previousRachet = session.GetChain(ratchet.LastRemoteEphemeralKey);
            if (previousRachet != null)
            {
                FillMessageKeys(previousRachet, previousCounter);
                previousRachet.ChainKey.Key = null; // Close
            }
            CalculateRatchet(session, remoteKey, false);

            var prevCounter = session.GetChain(ratchet.EphemeralKeyPair.Public);
            if (prevCounter != null)
            {
                ratchet.PreviousCounter = prevCounter.ChainKey.Counter;
                session.DeleteChain(ratchet.EphemeralKeyPair.Public);
            }
            ratchet.EphemeralKeyPair = EncryptionHelper.GenerateKeyPair();
            CalculateRatchet(session, remoteKey, true);
            ratchet.LastRemoteEphemeralKey = remoteKey.ToByteArray();
        }

        private void CalculateRatchet(Session session, ByteString remoteKey, bool sending)
        {
            var ratchet = session.CurrentRatchet;
            var sharedSecret = EncryptionHelper.CalculateAgreement(remoteKey.ToByteArray(), ratchet.EphemeralKeyPair.Private);
            var masterKey = EncryptionHelper.DeriveSecrets(sharedSecret, ratchet.RootKey, Encoding.UTF8.GetBytes("WhisperRatchet"), 2);

            var chainKey = sending ? ratchet.EphemeralKeyPair.Public : remoteKey.ToByteArray();
            session.Chains.Add(chainKey.ToByteString().ToBase64(), new Chain()
            {
                ChainKey = new ChainKey()
                {
                    Counter = -1,
                    Key = masterKey[1],
                },
                ChainType = sending ? ChainType.SENDING : ChainType.RECEIVING
            });
            ratchet.RootKey = masterKey[0];
        }

        private void FillMessageKeys(Chain chain, uint counter)
        {
            if (chain.ChainKey.Counter >= counter)
                return;

            if (counter - chain.ChainKey.Counter > 2000)
            {
                throw new SessionException("Over 2000 messages into the future!");
            }
            if (chain.ChainKey.Key == null)
            {
                throw new SessionException("Chain closed");
            }
            var key = chain.ChainKey.Key;
            chain.MessageKeys[chain.ChainKey.Counter + 1] = EncryptionHelper.CalculateMAC(key, [1]);
            chain.ChainKey.Key = EncryptionHelper.CalculateMAC(key, [2]);
            chain.ChainKey.Counter += 1;
            FillMessageKeys(chain, counter);
        }

        internal byte[] DecryptWhisperMessage(byte[] data)
        {
            return null;
        }

        private int[] decodeTupleByte(byte buff)
        {
            return [buff >> 4, buff & 0xf];
        }
    }
}
