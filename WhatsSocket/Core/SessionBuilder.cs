using Google.Protobuf;
using System.Diagnostics;
using System.Text;
using Textsecure;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.Stores;

namespace WhatsSocket.Core
{
    public class SessionBuilder
    {
        public SessionBuilder(SessionStore storage, ProtocolAddress address)
        {
            Storage = storage;
            Address = address;
        }

        public SessionStore Storage { get; }
        public ProtocolAddress Address { get; }


        public int InitIncoming(SessionRecord record, PreKeyWhisperMessage message)
        {
            var fqAddr = Address.ToString();
            if (Storage.IsTrustedIdentity(fqAddr, message.IdentityKey))
            {

            }
            var session = record.getSession(message.BaseKey.ToBase64());
            if (session != null)
            {
                return 0;
            }
            var preKeyPair = Storage.LoadPreKey(message.PreKeyId);
            Debug.WriteLine("preKeyPair:" + preKeyPair.Public.ToBase64());

            if (message.HasPreKeyId && preKeyPair == null)
            {
                throw new Exception("Invalid PreKey ID");
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

            return (int)message.PreKeyId;
        }

        private Session InitSession(bool isInitiator, KeyPair ourEphemeralKey, KeyPair ourSignedKey, ByteString theirIdentityPubKey, ByteString theirEphemeralPubKey, ByteString theirSignedPubKey, uint registrationId)
        {
            if (isInitiator)
            {
                if (ourSignedKey != null)
                {
                    throw new Exception("Invalid call to initSession");
                }
                ourSignedKey = ourEphemeralKey;
            }
            else
            {
                if (theirSignedPubKey != null)
                {
                    throw new Exception("Invalid call to initSession");
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

            var session = new Session();


            var ourIdentityKey = Storage.GetOurIdentity();
            var a1 = EncryptionHelper.CalculateAgreement(theirSignedPubKey.ToByteArray(), ourIdentityKey.Private);
            var a2 = EncryptionHelper.CalculateAgreement(theirIdentityPubKey.ToByteArray(), ourSignedKey.Private);
            var a3 = EncryptionHelper.CalculateAgreement(theirSignedPubKey.ToByteArray(), ourSignedKey.Private);

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
                var a4 = EncryptionHelper.CalculateAgreement(theirEphemeralPubKey.ToByteArray(), ourEphemeralKey.Private);
                Array.Copy(a4, 0, sharedSecret, 32 * 4, 32);
            }

            byte[][] masterKey = EncryptionHelper.DeriveSecrets(sharedSecret, new byte[32], Encoding.UTF8.GetBytes("WhisperText"));

            session.RegistrationId = registrationId;
            session.CurrentRatchet = new CurrentRatchet()
            {
                RootKey = masterKey[0],
                EphemeralKeyPair = isInitiator ? EncryptionHelper.GenerateKeyPair() : ourSignedKey,
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
            var sharedSecret = EncryptionHelper.SharedKey(remoteKey, ratchet.EphemeralKeyPair.Private);
            var masterKey = EncryptionHelper.DeriveSecrets(sharedSecret, ratchet.RootKey, Encoding.UTF8.GetBytes("WhisperText"));
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
