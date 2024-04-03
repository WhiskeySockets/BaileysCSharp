using Proto;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Stores;
using WhatsSocket.LibSignal;
using static WhatsSocket.Core.WABinary.JidUtils;

namespace WhatsSocket.Core.Signal
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
            var session = new GroupCipher(Auth.Keys, senderName);
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
            var builder = new GroupSessionBuilder(Auth.Keys);
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

    }
}
