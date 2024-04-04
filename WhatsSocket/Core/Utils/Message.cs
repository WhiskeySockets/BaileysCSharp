using Org.BouncyCastle.Tls;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.WABinary;
using static Proto.Message.Types;
using static WhatsSocket.Core.WABinary.JidUtils;
using static WhatsSocket.Core.Utils.GenericUtils;

namespace WhatsSocket.Core.Utils
{
    public class MessageUtil
    {
        public static Message NormalizeMessageContent(Message? content)
        {
            if (content == null)
                return null;

            // set max iterations to prevent an infinite loop
            for (var i = 0; i < 5; i++)
            {
                var inner = GetFutureProofMessage(content);

                if (inner == null)
                {
                    break;

                }

                content = inner.Message;
            }
            return content;
        }



        public static FutureProofMessage? GetFutureProofMessage(Message content)
        {
            return content.EphemeralMessage ??
              content.ViewOnceMessage ??
              content.DocumentWithCaptionMessage ??
              content.ViewOnceMessageV2 ??
              content.ViewOnceMessageV2Extension ??
              content.EditedMessage;
        }

        internal static PropertyInfo? GetContentType(Message content)
        {
            if (content == null)
            {
                return null;
            }
            if (content.SenderKeyDistributionMessage != null)
                return null;

            var type = content.GetType();
            var keys = type.GetProperties().Where(x => (x.Name == "Conversation" || (x.Name.Contains("Message")))).ToArray();
            foreach (var key in keys)
            {
                if (key.PropertyType == typeof(bool))
                {
                    continue;
                }

                var value = key.GetValue(content, null);
                if (value != null && value?.ToString() != "")
                {
                    var propertyName = key.Name.Replace("Has", "");
                    var property = type.GetProperty(propertyName);
                    return property;
                }
            }


            return null;
        }


        public static WebMessageInfo GenerateWAMessageFromContent(string jid, Message message, MessageGenerationOptionsFromContent? options = null)
        {
            var webmessage = new WebMessageInfo();
            if (options?.Quoted != null)
            {
                var quoted = options.Quoted;
                var participant = "";
                if (quoted.Key.FromMe)
                {
                    participant = options.UserJid;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(quoted.Participant))
                    {
                        participant = quoted.Participant;
                    }
                    else if (!string.IsNullOrWhiteSpace(quoted.Key.Participant))
                    {
                        participant = quoted.Key.Participant;
                    }
                    else
                    {
                        participant = quoted.Key.RemoteJid;
                    }
                }

                var quotedMsg = NormalizeMessageContent(quoted.Message);
                var contentType = GetContentType(quotedMsg);
                if (contentType != null)
                {
                    var valueToKeep = contentType.GetValue(quotedMsg, null);
                    quotedMsg = new Message();
                    contentType.SetValue(quotedMsg, valueToKeep);
                }
                var contextInfo = new ContextInfo()
                {
                    Participant = JidNormalizedUser(participant),
                    StanzaId = quoted.Key.Id,
                    QuotedMessage = quotedMsg,
                };
                // if a participant is quoted, then it must be a group
                // hence, remoteJid of group must also be entered
                if (jid != quoted?.Key?.RemoteJid)
                {
                    contextInfo.RemoteJid = quoted.Key.RemoteJid;
                }
                message.SetContextInfo(contextInfo);




            }

            webmessage.Key = new MessageKey()
            {
                FromMe = true,
                Id = GenerateMessageID(),
                RemoteJid = jid,
            };
            webmessage.Message = message;
            webmessage.MessageTimestamp = (ulong)(DateTimeOffset.Now.ToUnixTimeSeconds());
            webmessage.Status = WebMessageInfo.Types.Status.Pending;


            return webmessage;
        }


        public static WebMessageInfo GenerateWAMessage(string jid, AnyContentMessageModel content, MessageGenerationOptionsFromContent? options = null)
        {
            return GenerateWAMessageFromContent(jid, GenerateWAMessageContent(content, options), options);
        }


        public static Message GenerateWAMessageContent<T>(T message, MessageGenerationOptionsFromContent? options = null) where T : AnyContentMessageModel
        {
            var m = new Message();

            if (message is ExtendedTextMessageModel text)
            {
                m.ExtendedTextMessage = new ExtendedTextMessage()
                {
                    Text = text.Text,
                };

                ///TODO generateLinkPreviewIfRequired
            }
            //contacts
            //location
            //contacts
            //react
            //delete
            //forward
            //disappearingMessagesInChat
            //buttonReply
            //product
            //listReply
            //poll
            //sharePhoneNumber
            //requestPhoneNumber



            //Buttons

            //Sections

            if (message is IViewOnce viewOnce && viewOnce.ViewOnce)
            {
                m = new Message()
                {
                    ViewOnceMessage =
                    {
                        Message = m
                    }
                };
            }

            // Works
            if (message is IMentionable mentionable && mentionable.Mentions?.Length > 0)
            {
                var contentType = GetContentType(m);
                if (contentType != null)
                {
                    var contextInfo = new ContextInfo();
                    contextInfo.MentionedJid.AddRange(mentionable.Mentions);
                    m.SetContextInfo(contextInfo);
                }
            }

            return m;
        }
    }
}
