using Proto;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Stores;
using static WhatsSocket.Core.WABinary.JidUtils;

namespace WhatsSocket.Core.Signal
{
    public class SignalRepository
    {
        public SignalRepository(AuthenticationState auth)
        {
            Auth = auth;
        }
        public AuthenticationState Auth { get; }

        public byte[] decryptGroupMessage(string group, string authorJid, byte[] content)
        {
            var senderName = JidToSignalSenderKeyName(group, authorJid);
            var session = new GroupCipher(Auth.Keys, senderName);
            return session.Decrypt(content);
        }
        public byte[] decryptMessage(string user, string type, byte[] ciphertext)
        {
            var addr = new ProtocolAddress(JidDecode(user));
            var session = new SessionCipher(Auth, addr);
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
    }
}
