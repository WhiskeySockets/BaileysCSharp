using Proto;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using static BaileysCSharp.Core.Utils.JidUtils;
using BaileysCSharp.Core.WABinary;
using System.Text;
using BaileysCSharp.Core.Extensions;

namespace BaileysCSharp.Core.Signal
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

                        if (node.tag != "enc" && node.tag != "plaintext")
                            continue;

                        if (node.content is byte[] buffer)
                        {
                            decryptables += 1;
                            byte[] msgBuffer = default;
                            var e2eType = node.getattr("type") ?? node.tag ?? "none";
                            switch (e2eType)
                            {
                                case "skmsg":
                                    msgBuffer = Repository.DecryptGroupMessage(Sender, Author, buffer);
                                    break;
                                case "pkmsg":
                                case "msg":
                                    var user = IsJidUser(Sender) ? Sender : Author;
                                    msgBuffer = Repository.DecryptMessage(user, e2eType, buffer);
                                    break;
                                default:
                                case "plaintext":
                                    msgBuffer = buffer;
                                    break;
                            }

                            var msg = Message.Parser.ParseFrom(node.tag == "plaintext" ? msgBuffer : msgBuffer.UnpadRandomMax16());
                            msg = msg.DeviceSentMessage?.Message ?? msg;
                            if (msg.SenderKeyDistributionMessage != null)
                            {
                                Repository.ProcessSenderKeyDistributionMessage(Author, msg.SenderKeyDistributionMessage);
                            }
                            Msg.MessageTimestamp = Stanza.getattr("t").ToUInt64();
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
