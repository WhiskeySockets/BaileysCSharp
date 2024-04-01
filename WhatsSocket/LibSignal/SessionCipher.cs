using Google.Protobuf;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Textsecure;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Signal;
using WhatsSocket.Exceptions;
using WhatsSocket.LibSignal;

namespace WhatsSocket.LibSignal
{

    public class SessionCipher
    {
        public SessionCipher(SignalStorage storage, ProtocolAddress address)
        {
            Storage = storage;
            Address = address;
        }

        public SignalStorage Storage { get; }
        public ProtocolAddress Address { get; }


        public SessionRecord? GetRecord()
        {
            var record = Storage.LoadSession(Address);
            return record;
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


        internal byte[] DecryptWhisperMessage(byte[] data)
        {
            var record = GetRecord();
            var result = DecryptWithSessions(data, record.GetSessions());
            var remoteIdentityKey = result.Session.IndexInfo.RemoteIdentityKey;

            if (record.IsClosed(result.Session))
            {
                // It's possible for this to happen when processing a backlog of messages.
                // The message was, hopefully, just sent back in a time when this session
                // was the most current.  Simply make a note of it and continue.  If our
                // actual open session is for reason invalid, that must be handled via
                // a full SessionError response.
            }
            StoreRecord(record);
            return result.PlainText;
        }

        private SessionDecryptResult DecryptWithSessions(byte[] data, List<Session> sessions)
        {
            if (sessions.Count == 0)
            {
                throw new SessionException("No Sessions Available");
            }
            List<Exception> errors = new List<Exception>();
            foreach (var session in sessions)
            {
                try
                {
                    var plaintext = DoDecryptWhisperMessage(data, session);
                    session.IndexInfo.Used = DateTime.Now.AsEpoch();
                    return new SessionDecryptResult()
                    {
                        PlainText = plaintext,
                        Session = session
                    };
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }
            throw new Exception("No matching sessions found for message");
        }

        private void StoreRecord(SessionRecord record)
        {
            record.RemoveOldSessions();
            Storage.StoreSession(Address, record);
        }

        private byte[] DoDecryptWhisperMessage(byte[] messageBuffer, Session? session)
        {
            var versions = decodeTupleByte(messageBuffer[0]);

            var messageProto = messageBuffer.Skip(1).Take(messageBuffer.Length - 1 - 8).ToArray();
            var message = WhisperMessage.Parser.ParseFrom(messageProto);

            MaybeStepRatchet(session, message.EphemeralKey, (int)message.PreviousCounter);
            var chain = session.GetChain(message.EphemeralKey.ToByteArray());
            if (chain.ChainType == ChainType.SENDING)
            {
                throw new SessionException("Tried to decrypt on a sending chain");
            }
            FillMessageKeys(chain, (int)message.Counter);
            if (!chain.MessageKeys.ContainsKey((int)message.Counter))
            {
                throw new SessionException("Key used already or never filled");
            }
            var messageKey = chain.MessageKeys[(int)message.Counter];
            chain.MessageKeys.Remove((int)message.Counter);
            var keys = CryptoUtils.DeriveSecrets(messageKey, new byte[32], Encoding.UTF8.GetBytes("WhisperMessageKeys"));


            var ourIdentityKey = Storage.GetOurIdentity();
            var macInput = new byte[messageProto.Length + 33 * 2 + 1];
            macInput.Set(session.IndexInfo.RemoteIdentityKey);
            macInput.Set(ourIdentityKey.Public, 33);
            macInput[33 * 2] = EncodeTupleByte(3, 3);
            macInput.Set(messageProto, 33 * 2 + 1);

            CryptoUtils.VerifyMac(macInput, keys[1], messageBuffer.Skip(messageBuffer.Length - 8).ToArray(), 8);

            var plaintext = CryptoUtils.DecryptAesCbcWithIV(message.Ciphertext.ToByteArray(), keys[0], keys[2].Take(16).ToArray());
            session.PendingPreKey = null;
            return plaintext;
        }

        private byte EncodeTupleByte(int number1, int number2)
        {
            if (number1 > 15 || number2 > 15)
            {
                throw new SessionException("Numbers must be 4 bits or less");
            }
            return (byte)(number1 << 4 | number2);
        }

        private void MaybeStepRatchet(Session? session, ByteString remoteKey, int previousCounter)
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
            ratchet.EphemeralKeyPair = Curve.GenerateKeyPair();
            CalculateRatchet(session, remoteKey, true);
            ratchet.LastRemoteEphemeralKey = remoteKey.ToByteArray();
        }

        private void CalculateRatchet(Session session, ByteString remoteKey, bool sending)
        {
            var ratchet = session.CurrentRatchet;
            var sharedSecret = CryptoUtils.CalculateAgreement(remoteKey.ToByteArray(), ratchet.EphemeralKeyPair.Private);
            var masterKey = CryptoUtils.DeriveSecrets(sharedSecret, ratchet.RootKey, Encoding.UTF8.GetBytes("WhisperRatchet"), 2);

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

        private void FillMessageKeys(Chain chain, int counter)
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
            chain.MessageKeys[chain.ChainKey.Counter + 1] = CryptoUtils.CalculateMAC(key, [1]);
            chain.ChainKey.Key = CryptoUtils.CalculateMAC(key, [2]);
            chain.ChainKey.Counter += 1;
            FillMessageKeys(chain, counter);
        }


        private int[] decodeTupleByte(byte buff)
        {
            return [buff >> 4, buff & 0xf];
        }

        public EncryptData Encrypt(byte[] data)
        {
            var ourIdentityKey = Storage.GetOurIdentity();
            var record = GetRecord();
            if (record == null)
            {
                throw new SessionException("No Session");
            }

            var session = record.GetOpenSession();
            if (session == null)
            {
                throw new SessionException("No Open Session");
            }

            var remoteIdentity = session.IndexInfo.RemoteIdentityKey;
            if (remoteIdentity == null)
            {
                throw new SessonException("Untrusted Identity Key Error");
            }
            var chain = session.GetChain(session.CurrentRatchet.EphemeralKeyPair.Public);
            if (chain.ChainType == ChainType.RECEIVING)
            {
                throw new SessionException("Tried to encrypt on a receiving chain");
            }
            FillMessageKeys(chain, chain.ChainKey.Counter + 1);
            var keys = CryptoUtils.DeriveSecrets(chain.MessageKeys[chain.ChainKey.Counter], new byte[32], Encoding.UTF8.GetBytes("WhisperMessageKeys"));
            chain.MessageKeys.Remove(chain.ChainKey.Counter);
            //TODO
            WhisperMessage msg = new WhisperMessage();
            msg.EphemeralKey = session.CurrentRatchet.EphemeralKeyPair.Public.ToByteString();
            msg.Counter = (uint)chain.ChainKey.Counter;
            msg.PreviousCounter = (uint)session.CurrentRatchet.PreviousCounter;
            msg.Ciphertext = CryptoUtils.EncryptAesCbcWithIV(data, keys[0], keys[2].Slice(0, 16)).ToByteString();

            var msgBuf = msg.ToByteArray();
            var macInput = new byte[msgBuf.Length + (33 * 2) + 1];
            macInput.Set(ourIdentityKey.Public);
            macInput.Set(session.IndexInfo.RemoteIdentityKey, 33);
            macInput[33 * 2] = this.EncodeTupleByte(3, 3);
            macInput.Set(msgBuf, (33 * 2) + 1);
            var mac = CryptoUtils.CalculateMAC(keys[1], macInput);
            var result = new byte[msgBuf.Length + 9];
            result[0] = this.EncodeTupleByte(3, 3);
            result.Set(msgBuf, 1);
            result.Set(mac.Slice(0, 8), msgBuf.Length + 1);
            StoreRecord(record);
            var type = 1;
            byte[] body;

            if (session.PendingPreKey != null)
            {
                type = 3;
                var preKeyMsg = new PreKeyWhisperMessage()
                {
                    IdentityKey = ourIdentityKey.Public.ToByteString(),
                    RegistrationId = Storage.GetOurRegistrationId(),
                    BaseKey = session.PendingPreKey.BaseKey.ToByteString(),
                    SignedPreKeyId = session.PendingPreKey.SignedKeyId,
                    Message = result.ToByteString()
                };
                if (session.PendingPreKey.PreKeyId > 0)
                {
                    preKeyMsg.PreKeyId = session.PendingPreKey.PreKeyId;
                }
                body = new byte[] { EncodeTupleByte(3, 3) };
                body = body.Concat(preKeyMsg.ToByteArray()).ToArray();

            }
            else
            {
                type = 1;
                body = result;
            }
            return new EncryptData()
            {
                Type = type,
                Data = body,
                RegistrationId = session.RegistrationId
            };
        }
    }

}