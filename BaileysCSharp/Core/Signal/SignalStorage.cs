using Google.Protobuf;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.LibSignal;
using static BaileysCSharp.Core.Helper.CryptoUtils;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Signal
{
    public class SignalStorage
    {
        public SignalStorage(AuthenticationState auth)
        {
            Creds = auth.Creds;
            Keys = auth.Keys;
        }

        public AuthenticationCreds Creds { get; set; }
        public BaseKeyStore Keys { get; set; }


        public bool IsTrustedIdentity(string fqAddr, ByteString identityKey)
        {
            return true;
        }


        internal KeyPair LoadSignedPreKey(uint signedPreKeyId)
        {
            return Creds.SignedPreKey;
        }

        internal KeyPair GetOurIdentity()
        {
            return new KeyPair()
            {
                Private = Creds.SignedIdentityKey.Private,
                Public = GenerateSignalPubKey(Creds.SignedIdentityKey.Public),
            };
        }

        internal KeyPair LoadPreKey(uint preKeyId)
        {
            var result = Keys.Get<PreKeyPair>(preKeyId.ToString());
            if (result == null)
                return null;
            return result;
        }

        internal void RemovePreKey(uint preKeyId)
        {
            Keys.Set<PreKeyPair>(preKeyId.ToString(), null);
        }

        internal void StoreSenderKey(string senderName, SenderKeyRecord senderMsg)
        {
            Keys.Set(senderName, senderMsg);
            //SenderKeys.StoreSenderKey(senderName, senderMsg);
        }

        internal SenderKeyRecord LoadSenderKey(string senderName)
        {
            return Keys.Get<SenderKeyRecord>(senderName);
        }

        internal SessionRecord? LoadSession(ProtocolAddress address)
        {
            return Keys.Get<SessionRecord>(address.ToString());
        }

        internal void StoreSession(ProtocolAddress address, SessionRecord record)
        {
            Keys.Set(address.ToString(), record);
        }

        internal uint GetOurRegistrationId()
        {
            return (uint)Creds.RegistrationId;
        }
    }
}
