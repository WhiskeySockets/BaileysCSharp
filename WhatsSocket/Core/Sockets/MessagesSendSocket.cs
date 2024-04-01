using Proto;
using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using static WhatsSocket.Core.Utils.MessageUtil;
using static WhatsSocket.Core.Utils.GenericUtils;
using static WhatsSocket.Core.WABinary.JidUtils;
using static WhatsSocket.Core.WABinary.Constants;
using static WhatsSocket.Core.Utils.ValidateConnectionUtil;
using static WhatsSocket.Core.Utils.SignalUtils;
using Newtonsoft.Json;
using WhatsSocket.Core.Extensions;
using System.Collections.Generic;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.Signal;
using Google.Protobuf;
using WhatsSocket.Core.Events;
using WhatsSocket.LibSignal;

namespace WhatsSocket.Core.Sockets
{
    public class ParticipantNode
    {
        public BinaryNode[] Nodes { get; set; }
        public bool ShouldIncludeDeviceIdentity { get; set; }
    }

    public abstract class MessagesSendSocket : GroupSocket
    {
        public MessagesSendSocket([NotNull] SocketConfig config) : base(config)
        {
        }



        public async Task<WebMessageInfo?> SendMessage(string jid, AnyContentMessageModel content, MessageGenerationOptionsFromContent? options = null)
        {
            var userJid = Creds.Me.ID;
            if (content.DisappearingMessagesInChat.HasValue && JidUtils.IsJidGroup(jid))
            {
                ///TODO: groupToggleEphemeral
                ///
                return null;
            }
            else
            {
                options = options ?? new MessageGenerationOptionsFromContent();
                options.UserJid = userJid;
                options.Logger = Logger;

                var fullMsg = GenerateWAMessage(jid, content, options);

                var deleteModel = content as DeleteMessageModel;

                var editMessage = false; //TODO Handle Edit Message
                var additionalAttributes = new Dictionary<string, string>();


                // required for delete
                if (deleteModel != null)
                {
                    if (IsJidGroup(deleteModel.RemoteJid) && !deleteModel.FormMe)
                    {
                        additionalAttributes["edit"] = "8";
                    }
                    else
                    {
                        additionalAttributes["edit"] = "7";
                    }
                }


                await RelayMessage(jid, fullMsg.Message, new MessageRelayOptions()
                {
                    MessageID = fullMsg.Key.Id,
                    StatusJidList = options.StatusJidList,
                    AdditionalAttributes = additionalAttributes,
                    //cachedGroupMetadata
                });


                await UpsertMessage(fullMsg, MessageUpsertType.Append);

                return fullMsg;
            }

        }

        public async Task RelayMessage(string jid, Message message, MessageRelayOptions options)
        {
            var meId = Creds.Me.ID;

            var shouldIncludeDeviceIdentity = false;
            var jidDecoded = JidDecode(jid);
            var statusId = "status@broadcast";
            var isGroup = jidDecoded.Server == "g.us";
            var isStatus = jid == statusId;
            var isLid = jidDecoded.Server == "lid";

            options.MessageID = options.MessageID ?? GenerateMessageID();
            options.UseUserDevicesCache = options.UseUserDevicesCache != false;


            var participants = new List<BinaryNode>();
            var destinationJid = isStatus ? statusId : JidEncode(jidDecoded.User, isLid ? "lid" : isGroup ? "g.us" : "s.whatsapp.net");
            var binaryNodeContent = new List<BinaryNode>();
            var devices = new List<JidWidhDevice>();

            var meMsg = new Message()
            {
                DeviceSentMessage = new Message.Types.DeviceSentMessage()
                {
                    DestinationJid = jid,
                    Message = message
                }
            };

            if (options.Participant != null)
            {
                // when the retry request is not for a group
                // only send to the specific device that asked for a retry
                // otherwise the message is sent out to every device that should be a recipient
                if (!isGroup && !isStatus)
                {
                    options.AdditionalAttributes["device_fanout"] = "false";
                }

                var participantJidDecoded = JidDecode(options.Participant.Jid);
                devices.Add(new JidWidhDevice()
                {
                    Device = participantJidDecoded.Device,
                    User = participantJidDecoded.User,
                });
            }


            //Transaction thingy ?
            var mediaType = GetMediaType(message);
            if (isGroup || isStatus) // Group and Status
            {
                //TODO: handle status and group
            }
            else
            {
                var me = JidDecode(meId);
                if (options.Participant == null)
                {
                    devices.Add(new JidWidhDevice() { User = jidDecoded.User });
                    // do not send message to self if the device is 0 (mobile)
                    if (me.Device != 0)
                    {
                        devices.Add(new JidWidhDevice() { User = me.User });
                    }

                    var additionalDevices = await GetUSyncDevices([meId, jid], options.UseUserDevicesCache ?? false, true);
                    devices.AddRange(additionalDevices);
                }

                List<string> allJids = new List<string>();
                List<string> meJids = new List<string>();
                List<string> otherJids = new List<string>();

                foreach (var item in devices)
                {
                    var isMe = item.User == me.User;
                    var addJid = JidEncode((isMe && isLid) ? Creds.Me.LID.Split(":")[0] ?? item.User : item.User, isLid ? "lid" : "s.whatsapp.net", item.Device);
                    if (isMe)
                    {
                        meJids.Add(addJid);
                    }
                    else
                    {
                        otherJids.Add(addJid);
                    }
                    allJids.Add(addJid);
                }

                await AssertSessions(allJids, false);

                //TODO Add Media Type
                var meNode = CreateParticipantNodes(meJids.ToArray(), meMsg, null);
                var otherNode = CreateParticipantNodes(otherJids.ToArray(), message, null);

                participants.AddRange(meNode.Nodes);
                participants.AddRange(otherNode.Nodes);
                shouldIncludeDeviceIdentity = shouldIncludeDeviceIdentity || meNode.ShouldIncludeDeviceIdentity || otherNode.ShouldIncludeDeviceIdentity;

                if (participants.Count > 0)
                {
                    binaryNodeContent.Add(new BinaryNode()
                    {
                        tag = "participants",
                        content = participants.ToArray()
                    });
                }

                var stanza = new BinaryNode()
                {
                    tag = "message",
                    attrs =
                    {
                        {"id",options.MessageID  },
                        {"type" , "text" }
                    }
                };
                if (options.AdditionalAttributes != null)
                {
                    foreach (var item in options.AdditionalAttributes)
                    {
                        stanza.attrs.Add(item.Key, item.Value);
                    }
                }

                // if the participant to send to is explicitly specified (generally retry recp)
                // ensure the message is only sent to that person
                // if a retry receipt is sent to everyone -- it'll fail decryption for everyone else who received the msg
                if (options.Participant != null)
                {
                    if (IsJidGroup(destinationJid))
                    {
                        stanza.attrs["to"] = destinationJid;
                        stanza.attrs["participant"] = options.Participant.Jid;
                    }
                    else if (AreJidsSameUser(options.Participant.Jid, meId))
                    {
                        stanza.attrs["to"] = options.Participant.Jid;
                        stanza.attrs["participant"] = destinationJid;
                    }
                    else
                    {
                        stanza.attrs["to"] = options.Participant.Jid;
                    }
                }
                else
                {
                    stanza.attrs["to"] = destinationJid;
                }

                if (shouldIncludeDeviceIdentity)
                {
                    binaryNodeContent.Add(new BinaryNode()
                    {
                        tag = "device-identity",
                        attrs = { },
                        content = EncodeSignedDeviceIdentity(Creds.Account, true)
                    });
                }
                stanza.content = binaryNodeContent.ToArray();

                //TODO: Button Type

                Logger.Debug(new { msgId = options.MessageID }, $"sending message to ${participants.Count} devices");


                SendNode(stanza);
            }

        }

        public ParticipantNode CreateParticipantNodes(string[] jids, Message message, Dictionary<string, string> attrs)
        {
            ParticipantNode result = new ParticipantNode();
            var patched = SocketConfig.PatchMessageBeforeSending(message, jids);
            var bytes = EncodeWAMessage(patched);//.ToByteArray();

            result.ShouldIncludeDeviceIdentity = false;
            List<BinaryNode> nodes = new List<BinaryNode>();

            foreach (var jid in jids)
            {
                var enc = Repository.EncryptMessage(jid, bytes);
                if (enc.Type == "pkmsg")
                {
                    result.ShouldIncludeDeviceIdentity = true;
                }
                var encNode = new BinaryNode()
                {
                    tag = "enc",
                    attrs =
                            {
                                {"v","2" },
                                {"type",enc.Type },
                            },
                    content = enc.CipherText
                };
                if (attrs != null)
                {
                    foreach (var attr in attrs)
                    {
                        encNode.attrs[attr.Key] = attr.Value;
                    }
                }
                var node = new BinaryNode()
                {
                    tag = "to",
                    attrs = { { "jid", jid } },
                    content = new BinaryNode[]
                    {
                        encNode
                    }
                };
                nodes.Add(node);
            }

            result.Nodes = nodes.ToArray();

            return result;
        }

        protected async Task<bool> AssertSessions(List<string> jids, bool force)
        {
            var didFetchNewSession = false;
            List<string> jidsRequiringFetch = new List<string>();
            if (force)
            {
                jidsRequiringFetch = jids.ToList();
            }
            else
            {
                var addrs = jids.Select(x => new ProtocolAddress(x)).ToList();
                var sessions = Keys.Get<SessionRecord>(addrs.Select(x => x.ToString()).ToList());
                foreach (var jid in jids)
                {
                    if (!sessions.ContainsKey(new ProtocolAddress(jid).ToString()))
                    {
                        jidsRequiringFetch.Add(jid.ToString());
                    }
                    else if (sessions[new ProtocolAddress(jid).ToString()] == null)
                    {
                        jidsRequiringFetch.Add(jid.ToString());
                    }
                }
            }

            if (jidsRequiringFetch.Count > 0)
            {
                Logger.Debug(new { jidsRequiringFetch }, "fetching sessions");
                var result = await Query(new BinaryNode()
                {
                    tag = "iq",
                    attrs =
                    {
                        {"xmlns", "encrypt" },
                        {"type", "get" },
                        {"to", S_WHATSAPP_NET }
                    },
                    content = new BinaryNode[]
                    {
                        new BinaryNode()
                        {
                            tag = "key",
                            attrs = { },
                            content = jidsRequiringFetch.Select(x => new BinaryNode()
                            {
                                tag = "user",
                                attrs =
                                {
                                    {"jid",x }
                                }

                            }).ToArray()
                        }
                    }
                });

                ParseAndInjectE2ESessions(result, Repository);

                didFetchNewSession = true;
            }


            return didFetchNewSession;
        }

        NodeCache userDevicesCache = new NodeCache();

        private async Task<List<JidWidhDevice>> GetUSyncDevices(string[] jids, bool useCache, bool ignoreZeroDevices)
        {
            var deviceResults = new List<JidWidhDevice>();
            if (!useCache)
            {
                Logger.Debug("not using cache for devices");
            }

            var users = new List<BinaryNode>();
            foreach (var jid in jids)
            {
                var user = JidDecode(jid).User;
                var normalJid = JidNormalizedUser(jid);

                var devices = userDevicesCache.Get<JidWidhDevice[]>(jid);
                if (devices != null && devices.Length > 0 && useCache)
                {
                    deviceResults.AddRange(devices);
                    Logger.Trace(new { user }, "using cache for devices");
                }
                else
                {
                    users.Add(new BinaryNode("user")
                    {
                        attrs = { { "jid", normalJid } }
                    });
                }
            }

            var iq = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to", S_WHATSAPP_NET },
                    {"type" , "get" },
                    {"xmlns","usync" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "usync",
                        attrs =
                        {
                            {"sid", GenerateMessageTag() },
                            {"mode","query" },
                            //{"mode","usync" },
                            {"last","true" },
                            {"index","0" },
                            {"context","message" }
                        },
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "query",
                                content = new BinaryNode[]
                                {
                                    new BinaryNode()
                                    {
                                        tag = "devices",
                                        attrs =
                                        {
                                            {"version", "2" }
                                        }
                                    }
                                },
                            },
                            new BinaryNode()
                            {
                                tag ="list",
                                content = users.ToArray()
                            }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(iq, Formatting.Indented);


            var result = await Query(iq);

            var extracted = ExtractDeviceJids(result, Creds.Me.ID, ignoreZeroDevices);

            Dictionary<string, List<JidWidhDevice>> deviceMap = new Dictionary<string, List<JidWidhDevice>>();

            foreach (var item in extracted)
            {
                deviceMap[item.User] = deviceMap.ContainsKey(item.User) == true ? deviceMap[item.User] : new List<JidWidhDevice>();

                deviceMap[item.User].Add(item);
                deviceResults.Add(item);
            }

            foreach (var item in deviceMap)
            {
                userDevicesCache.Set(item.Key, item.Value);
            }


            return deviceResults;
        }


        public string GetMediaType(Message message)
        {
            if (message.ImageMessage != null)
            {
                return "image";
            }
            else if (message.VideoMessage != null)
            {
                return message.VideoMessage.GifPlayback ? "gif" : "video";
            }
            else if (message.AudioMessage != null)
            {
                return message.AudioMessage.Ptt ? "ptt" : "audio";
            }
            else if (message.ContactMessage != null)
            {
                return "vcard";
            }
            else if (message.DocumentMessage != null)
            {
                return "document";
            }
            else if (message.ContactsArrayMessage != null)
            {
                return "contact_array";
            }
            else if (message.LiveLocationMessage != null)
            {
                return "livelocation";
            }
            else if (message.StickerMessage != null)
            {
                return "sticker";
            }
            else if (message.ListMessage != null)
            {
                return "list";
            }
            else if (message.ListResponseMessage != null)
            {
                return "list_response";
            }
            else if (message.ButtonsResponseMessage != null)
            {
                return "buttons_response";
            }
            else if (message.OrderMessage != null)
            {
                return "order";
            }
            else if (message.ProductMessage != null)
            {
                return "product";
            }
            else if (message.InteractiveResponseMessage != null)
            {
                return "native_flow_response";
            }
            else
            {
                return "unknown"; // Or handle any other cases accordingly
            }
        }
    }
}
