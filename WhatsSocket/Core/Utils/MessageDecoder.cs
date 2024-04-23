using Org.BouncyCastle.Cms;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.Signal;
using BaileysCSharp.Exceptions;
using static BaileysCSharp.Core.WABinary.JidUtils;

namespace BaileysCSharp.Core
{

    public class MessageDecoder
    {
        public static MessageDecryptor DecryptMessageNode(BinaryNode stanza, string meId, string meLid, SignalRepository repository, Logger logger)
        {

            string chatId = "";
            string msgType = "";
            string author = "";

            var msgId = stanza.attrs["id"];
            var from = stanza.attrs["from"];
            var participant = stanza.getattr("participant");
            var recipient = stanza.getattr("recipient");

            if (IsJidUser(from))
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    if (!AreJidsSameUser(from, meId))
                    {
                        throw new Boom("receipient present, but msg not from me", Events.DisconnectReason.MissMatch);
                    }
                    chatId = recipient;
                }
                else
                {
                    chatId = from;
                }
                msgType = "chat";
                author = from;
            }
            else if (IsLidUser(from))
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                {
                    if (!AreJidsSameUser(from, meLid))
                    {
                        throw new Boom("receipient present, but msg not from me", Events.DisconnectReason.MissMatch);
                    }
                    chatId = recipient;
                }
                else
                {
                    chatId = from;
                }
                msgType = "chat";
                author = from;
            }
            else if (IsJidGroup(from))
            {
                if (participant == null)
                {
                    throw new Boom("No participant in group message", Events.DisconnectReason.MissMatch);
                }
                else
                {
                    msgType = "group";
                    author = participant;
                    chatId = from;
                }
            }
            else if (IsBroadcast(from))
            {
                if (participant == null)
                {
                    throw new Boom("No participant in group message", Events.DisconnectReason.MissMatch);
                }
                else
                {
                    var isParticipantMe = AreJidsSameUser(meId, participant);

                    if (IsJidStatusBroadcast(from))
                    {
                        msgType = isParticipantMe ? "direct_peer_status" : "other_status";
                    }
                    else
                    {
                        msgType = isParticipantMe ? "peer_broadcast" : "other_broadcast";
                    }

                    chatId = from;
                    author = participant;
                }
            }

            var notify = stanza.getattr("notify");
            var fromMe = IsLidUser(from) ? AreJidsSameUser(meLid, !string.IsNullOrWhiteSpace(participant) ? participant : from) : AreJidsSameUser(meId, !string.IsNullOrWhiteSpace(participant) ? participant : from);

            var fullMessage = new WebMessageInfo()
            {
                Key = new MessageKey()
                {
                    RemoteJid = chatId,
                    Id = msgId,
                    FromMe = fromMe,
                    Participant = participant ?? "",
                },
                PushName = notify,
                Broadcast = IsBroadcast(from)

            };

            if (fromMe)
            {
                fullMessage.Status = WebMessageInfo.Types.Status.ServerAck;
            }


            return new MessageDecryptor(repository)
            {
                Stanza = stanza,
                Msg = fullMessage,
                Author = author,
                Category = stanza.getattr("category") ?? "",
                Sender = msgType == "chat" ? author : chatId
            };
        }

    }
}
