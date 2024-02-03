using Proto;
using WhatsSocket.Core.Curve;
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Helper;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static WhatsSocket.Core.Utils.JidUtils;

namespace WhatsSocket.Core
{

    public class MessageDecryptor
    {
        public MessageDecryptor(SignalRepository repository)
        {
            Repository = repository;
        }

        public BinaryNode Stanza { get; set; }
        public WebMessageInfo Msg { get; set; }
        public string Category { get; set; }
        public string Author { get; set; }
        public string Sender { get; set; }
        public SignalRepository Repository { get; }

        public void Decrypt()
        {
            int decryptables = 0;
            try
            {
                if (Stanza.content is BinaryNode[] nodes)
                {
                    foreach (var node in nodes)
                    {
                        if (node.tag == "verified_name" && node.content is byte[] bytes)
                        {
                            var cert = VerifiedNameCertificate.Parser.ParseFrom(bytes);
                            var details = VerifiedNameCertificate.Types.Details.Parser.ParseFrom(cert.Details);
                            Msg.VerifiedBizName = details.VerifiedName;
                        }

                        if (node.tag != "enc")
                            continue;

                        if (node.content is byte[] buffer)
                        {
                            decryptables += 1;
                            byte[] msgBuffer = default;
                            var e2eType = node.getattr("type") ?? "none";
                            switch (e2eType)
                            {
                                case "skmsg":
                                    msgBuffer = Repository.decryptGroupMessage(Sender, Author, buffer);
                                    break;
                                case "pkmsg":
                                case "msg":
                                    var user = IsJidUser(Sender) ? Sender : Author;
                                    msgBuffer = Repository.decryptMessage(user, e2eType, buffer);
                                    break;
                                default:
                                    break;
                            }


                            var msg = Message.Parser.ParseFrom(msgBuffer.UnpadRandomMax16());
                            msg = msg.DeviceSentMessage?.Message ?? msg;
                            if (msg.SenderKeyDistributionMessage != null)
                            {
                                Repository.ProcessSenderKeyDistributionMessage(Author, msg.SenderKeyDistributionMessage);
                            }

                            Msg.Message = msg;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Msg.MessageStubType = WebMessageInfo.Types.StubType.Ciphertext;
                Msg.MessageStubParameters.Add($"{ex.GetType().Name} - {ex.Message}");

            }
            if (decryptables == 0)
            {
                Msg.MessageStubType = WebMessageInfo.Types.StubType.Ciphertext;
                Msg.MessageStubParameters.Add("Message absent from node");
            }
        }
    }
}
