using Google.Protobuf;
using Proto;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.LibSignal;
using static BaileysCSharp.Core.WABinary.JidUtils;

namespace BaileysCSharp.Core.Signal
{
    public class SignalRepository
    {
        public SignalStorage Storage { get; set; }
        public SignalRepository(AuthenticationState auth)
        {
            Auth = auth;
            Storage = new SignalStorage(Auth);
        }
        public AuthenticationState Auth { get; }

        public byte[] DecryptGroupMessage(string group, string authorJid, byte[] content)
        {
            var senderName = JidToSignalSenderKeyName(group, authorJid);
            var session = new GroupCipher(Storage, senderName);
            return session.Decrypt(content);
        }

        public CipherMessage EncryptMessage(string jid, byte[] data)
        {
            var address = new ProtocolAddress(jid);
            var cipher = new SessionCipher(Storage, address);

            var enc = cipher.Encrypt(data);
            return new CipherMessage()
            {
                Type = enc.Type == 3 ? "pkmsg" : "msg",
                CipherText = enc.Data
            };
        }

        public byte[] DecryptMessage(string user, string type, byte[] ciphertext)
        {
            var addr = new ProtocolAddress(user);
            var session = new SessionCipher(Storage, addr);
            byte[] result;
            if (type == "pkmsg")
            {
                result = session.DecryptPreKeyWhisperMessage(ciphertext);
            }
            else
            {
                result = session.DecryptWhisperMessage(ciphertext);
            }
            return result;
        }

        public void ProcessSenderKeyDistributionMessage(string author, Message.Types.SenderKeyDistributionMessage senderKeyDistributionMessage)
        {
            var builder = new GroupSessionBuilder(Storage);
            var senderName = JidToSignalSenderKeyName(senderKeyDistributionMessage.GroupId, author);
            var senderMsg = Proto.SenderKeyDistributionMessage.Parser.ParseFrom(senderKeyDistributionMessage.AxolotlSenderKeyDistributionMessage.ToByteArray().Skip(1).ToArray());
            Auth.Keys.Set(senderName, new SenderKeyRecord());
            builder.Process(senderName, senderMsg);
        }

        internal void InjectE2ESession(string jid, E2ESession session)
        {
            var addr = new ProtocolAddress(jid);
            var sessionBuilder = new SessionBuilder(Storage, addr);
            sessionBuilder.InitOutGoing(session);
        }

        public GroupCipherMessage EncryptGroupMessage(string group, string meId, byte[] bytes)
        {
            var senderName = JidToSignalSenderKeyName(group, meId);
            var builder = new GroupSessionBuilder(Storage);

            var senderKey = Auth.Keys.Get<SenderKeyRecord>(senderName);
            if (senderKey == null)
            {
                Auth.Keys.Set(senderName, new SenderKeyRecord());
            }

            var senderKeyDistributionMessage = builder.Create(senderName);

            var session = new GroupCipher(Storage, senderName);
            var ciphertext = session.Encrypt(bytes);


            return new GroupCipherMessage()
            {
                CipherText = ciphertext,
                SenderKeyDistributionMessage = new byte[] { 51 }.Concat(senderKeyDistributionMessage.ToByteArray()).ToArray()
            };
        }
    }

    public class GroupCipherMessage
    {
        public byte[] CipherText { get; set; }
        public byte[] SenderKeyDistributionMessage { get; set; }
    }
}
