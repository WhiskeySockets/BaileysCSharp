using Proto;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;
using static WhatsSocket.Core.Encodings.JidUtils;

namespace WhatsSocket.Core
{
    public class SignalRepository
    {
        public SignalRepository(SessionStore storage)
        {
            Storage = storage;
        }

        public SessionStore Storage { get; }



        public byte[] decryptGroupMessage(string group, string authorJid, byte[] content)
        {
            return null;
        }
        public byte[] decryptMessage(string user, string type, byte[] ciphertext)
        {
            var addr = new ProtocolAddress(JidDecode(user));
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

        }
    }
}
