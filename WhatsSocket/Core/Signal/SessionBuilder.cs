using Google.Protobuf;
using System.Diagnostics;
using System.Text;
using Textsecure;
using WhatsSocket.Core.Curve;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Stores;

namespace WhatsSocket.Core.Signal
{

    public class SessionBuilder
    {
        public SessionBuilder(SignalStorage storage, ProtocolAddress address)
        {
            Storage = storage;
            Address = address;
        }

        public SignalStorage Storage { get; }
        public ProtocolAddress Address { get; }

        public void InitOutGoing(E2ESession device)
        {
            var fqAddr = Address.ToString();

            if (!Storage.IsTrustedIdentity(fqAddr, device.IdentityKey.ToByteString()))
            {

            }
            Curve25519.VerifySignature(device.IdentityKey, device.SignedPreKey.Public, device.SignedPreKey.Signature);

            var baseKey = CryptoUtils.GenerateKeyPair();
            var devicePreKey = device.PreKey != null && device.PreKey.Public != null ? device.PreKey.Public : null;
            var session = InitSession(true, baseKey, null, device.IdentityKey.ToByteString(), devicePreKey.ToByteString(), device.SignedPreKey.Public.ToByteString(), device.RegistrationId);


            session.PendingPreKey = new PendingPreKey()
            {
                SignedKeyId = device.SignedPreKey.KeyId,
                BaseKey = baseKey.Public,
            };
            if (device.PreKey != null)
            {
                session.PendingPreKey.PreKeyId = device.PreKey.KeyId;
            }
            var record = Storage.LoadSession(Address);
            if (record == null)
            {
                record = new SessionRecord();
            }
            else
            {
                var openSession = record.GetOpenSession();
                if (openSession != null)
                {
                    record.CloseSession(openSession);
                }
            }

            record.SetSession(session);
            Storage.StoreSession(Address, record);
        }

        public uint InitIncoming(SessionRecord record, PreKeyWhisperMessage message)
        {
            var fqAddr = Address.ToString();
            if (!Storage.IsTrustedIdentity(fqAddr, message.IdentityKey))
            {

            }
            var session = record.getSession(message.BaseKey.ToBase64());
            if (session != null)
            {
                return 0;
            }
            var preKeyPair = Storage.LoadPreKey(message.PreKeyId);

            if (message.HasPreKeyId && preKeyPair == null)
            {
                throw new SessonException("Invalid PreKey ID");
            }

            var signedPreKeyPair = Storage.LoadSignedPreKey(message.SignedPreKeyId);

            var existingOpenSession = record.GetOpenSession();
            if (existingOpenSession != null)
            {
                record.CloseSession(existingOpenSession);
            }

            record.SetSession(InitSession(false,
                preKeyPair, signedPreKeyPair,
                message.IdentityKey, message.BaseKey,
                null, message.RegistrationId));

            return message.PreKeyId;
        }

        private Session InitSession(bool isInitiator, KeyPair ourEphemeralKey, KeyPair ourSignedKey, ByteString theirIdentityPubKey, ByteString theirEphemeralPubKey, ByteString theirSignedPubKey, uint registrationId)
        {
            if (isInitiator)
            {
                if (ourSignedKey != null)
                {
                    throw new SessonException("Invalid call to initSession");
                }
                ourSignedKey = ourEphemeralKey;
            }
            else
            {
                if (theirSignedPubKey != null)
                {
                    throw new SessonException("Invalid call to initSession");
                }
                theirSignedPubKey = theirEphemeralPubKey; //1
            }

            byte[] sharedSecret = new byte[32 * 5];
            if (ourEphemeralKey == null || theirEphemeralPubKey == null)
            {
                sharedSecret = new byte[32 * 4];
            }
            for (var i = 0; i < 32; i++)
            {
                sharedSecret[i] = 0xff;
            }



            var ourIdentityKey = Storage.GetOurIdentity();
            var a1 = CryptoUtils.CalculateAgreement(theirSignedPubKey.ToByteArray(), ourIdentityKey.Private);
            var a2 = CryptoUtils.CalculateAgreement(theirIdentityPubKey.ToByteArray(), ourSignedKey.Private);
            var a3 = CryptoUtils.CalculateAgreement(theirSignedPubKey.ToByteArray(), ourSignedKey.Private);

            if (isInitiator)
            {
                Array.Copy(a1, 0, sharedSecret, 32, 32);
                Array.Copy(a2, 0, sharedSecret, 32 * 2, 32);
            }
            else
            {
                Array.Copy(a1, 0, sharedSecret, 32 * 2, 32);
                Array.Copy(a2, 0, sharedSecret, 32, 32);
            }
            Array.Copy(a3, 0, sharedSecret, 32 * 3, 32);
            if (ourEphemeralKey != null && theirEphemeralPubKey != null)
            {
                var a4 = CryptoUtils.CalculateAgreement(theirEphemeralPubKey.ToByteArray(), ourEphemeralKey.Private);
                Array.Copy(a4, 0, sharedSecret, 32 * 4, 32);
            }

            byte[][] masterKey = CryptoUtils.DeriveSecrets(sharedSecret, new byte[32], Encoding.UTF8.GetBytes("WhisperText"));

            var session = new Session();
            session.RegistrationId = registrationId;
            session.CurrentRatchet = new CurrentRatchet()
            {
                RootKey = masterKey[0],
                EphemeralKeyPair = isInitiator ? CryptoUtils.GenerateKeyPair() : ourSignedKey,
                LastRemoteEphemeralKey = theirSignedPubKey.ToByteArray(),
                PreviousCounter = 0
            };
            session.IndexInfo = new IndexInfo()
            {
                Created = DateTime.UtcNow.AsEpoch(),
                Used = DateTime.UtcNow.AsEpoch(),
                RemoteIdentityKey = theirIdentityPubKey.ToByteArray(),
                BaseKey = isInitiator ? ourEphemeralKey.Public : theirEphemeralPubKey.ToByteArray(),
                BaseKeyType = isInitiator ? BaseKeyType.OURS : BaseKeyType.THEIRS,
                Closed = -1
            };

            if (isInitiator)
            {
                // If we're initiating we go ahead and set our first sending ephemeral key now,
                // otherwise we figure it out when we first maybeStepRatchet with the remote's
                // ephemeral key
                CalculateSendingRatchet(session, theirSignedPubKey);
            }


            return session;
        }


        private void CalculateSendingRatchet(Session session, ByteString remoteKey)
        {
            var ratchet = session.CurrentRatchet;
            var sharedSecret = CryptoUtils.SharedKey(remoteKey, ratchet.EphemeralKeyPair.Private);
            var masterKey = CryptoUtils.DeriveSecrets(sharedSecret, ratchet.RootKey, Encoding.UTF8.GetBytes("WhisperRatchet"));
            session.Chains.Add(ratchet.EphemeralKeyPair.Public.ToByteString().ToBase64(), new Chain()
            {
                ChainKey = new ChainKey()
                {
                    Counter = -1,
                    Key = masterKey[2],
                },
                ChainType = ChainType.SENDING
            });
            ratchet.RootKey = masterKey[0];
        }

    }
}
