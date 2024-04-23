using Google.Protobuf;
using System;
using System.Diagnostics;
using System.Text;
using Textsecure;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Signal;

namespace BaileysCSharp.LibSignal
{

    public class SessionBuilder
    {
        public static KeyPair? OutKeyPair { get; set; }
        public static KeyPair? SendKeyPair { get; set; }

        public SessionBuilder(SignalStorage storage, ProtocolAddress address)
        {
            Storage = storage;
            Address = address;
        }

        public SignalStorage Storage { get; }
        public ProtocolAddress Address { get; }

        public SessionRecord InitOutGoing(E2ESession device)
        {
            var fqAddr = Address.ToString();

            if (!Storage.IsTrustedIdentity(fqAddr, device.IdentityKey.ToByteString()))
            {

            }
            Curve.VerifySignature(device.IdentityKey, device.SignedPreKey.Public, device.SignedPreKey.Signature);

            var baseKey = OutKeyPair ?? NodeCrypto.GenerateKeyPair();

            var devicePreKey = device.PreKey != null && device.PreKey.Public != null ? device.PreKey.Public : null;
            var session = InitSession(true, baseKey, null, device.IdentityKey,
                devicePreKey, device.SignedPreKey.Public,
                device.RegistrationId);


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
                    Debug.WriteLine("Closing stale open session for new outgoing prekey bundle");
                    record.CloseSession(openSession);
                }
            }

            record.SetSession(session);
            Storage.StoreSession(Address, record);
            return record;
        }

        private Session InitSession(bool isInitiator, KeyPair ourEphemeralKey, KeyPair ourSignedKey, byte[] theirIdentityPubKey, byte[]? theirEphemeralPubKey, byte[] theirSignedPubKey, uint registrationId)
        {
            return InitSession(isInitiator, ourEphemeralKey, ourSignedKey, theirIdentityPubKey.ToByteString(), theirEphemeralPubKey.ToByteString(), theirSignedPubKey.ToByteString(), registrationId);
        }

        public uint InitIncoming(SessionRecord record, PreKeyWhisperMessage message)
        {
            var fqAddr = Address.ToString();
            if (!Storage.IsTrustedIdentity(fqAddr, message.IdentityKey))
            {
                throw new UntrustedIdentityKeyError(fqAddr, message.IdentityKey);

            }
            var session = record.GetSession(message.BaseKey.ToBase64());
            if (session != null)
            {
                // This just means we haven't replied.
                return 0;
            }
            var preKeyPair = Storage.LoadPreKey(message.PreKeyId);

            if (message.HasPreKeyId && preKeyPair == null)
            {
                throw new SessonException("Invalid PreKey ID");
            }

            var signedPreKeyPair = Storage.LoadSignedPreKey(message.SignedPreKeyId);

            if (signedPreKeyPair == null)
            {
                throw new SessonException("InMissingvalid SignedPreKey");
            }

            var existingOpenSession = record.GetOpenSession();
            if (existingOpenSession != null)
            {
                Debug.WriteLine("Closing open session in favor of incoming prekey bundle");
                record.CloseSession(existingOpenSession);
            }

            record.SetSession(InitSession(false, preKeyPair, signedPreKeyPair,
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

            byte[] sharedSecret;
            if (ourEphemeralKey == null || theirEphemeralPubKey == null)
            {
                sharedSecret = new byte[32 * 4];
            }
            else
            {
                sharedSecret = new byte[32 * 5];
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
                sharedSecret.Set(a1, 32);
                sharedSecret.Set(a2, 32 * 2);
            }
            else
            {
                sharedSecret.Set(a1, 32 * 2);
                sharedSecret.Set(a2, 32);
            }
            sharedSecret.Set(a3, 32 * 3);
            if (ourEphemeralKey != null && theirEphemeralPubKey != null)
            {
                var a4 = CryptoUtils.CalculateAgreement(theirEphemeralPubKey.ToByteArray(), ourEphemeralKey.Private);
                sharedSecret.Set(a4, 32 * 4);
            }

            byte[][] masterKey = CryptoUtils.DeriveSecrets(sharedSecret, new byte[32], Encoding.UTF8.GetBytes("WhisperText"));

            var session = new Session();
            session.RegistrationId = registrationId;

            SendKeyPair = SendKeyPair ?? NodeCrypto.GenerateKeyPair();

            session.CurrentRatchet = new CurrentRatchet()
            {
                RootKey = masterKey[0],
                EphemeralKeyPair = isInitiator ? SendKeyPair : ourSignedKey,//32,32 or 32,33
                LastRemoteEphemeralKey = theirSignedPubKey.ToByteArray(),
                PreviousCounter = 0
            };
            session.IndexInfo = new IndexInfo()
            {
                Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Used = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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
            var sharedSecret = CryptoUtils.CalculateAgreement(remoteKey.ToByteArray(), ratchet.EphemeralKeyPair.Private);
            var masterKey = CryptoUtils.DeriveSecrets(sharedSecret, ratchet.RootKey, Encoding.UTF8.GetBytes("WhisperRatchet"));
            session.Chains.Add(ratchet.EphemeralKeyPair.Public.ToByteString().ToBase64(), new Chain()
            {
                MessageKeys = new Dictionary<int, byte[]>(),
                ChainKey = new ChainKey()
                {
                    Counter = -1,
                    Key = masterKey[1],
                },
                ChainType = ChainType.SENDING
            });
            ratchet.RootKey = masterKey[0];
        }

    }
}
