using Proto;
using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using static WhatsSocket.Core.Utils.MessageUtil;
using static WhatsSocket.Core.Utils.GenericUtils;
using static WhatsSocket.Core.WABinary.JidUtils;
using static WhatsSocket.Core.WABinary.Constants;
using WhatsSocket.Core.Delegates;
using Newtonsoft.Json;

namespace WhatsSocket.Core.Sockets
{
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

                var fullMsg = GenerateWAMessage(jid, content, options);

                //Handle Delete
                var isDeleteMsg = false;
                var isEditMsg = false;
                var additionalAttributes = new Dictionary<string, string>();

                await RelayMessage(jid, fullMsg.Message, new MessageRelayOptions()
                {
                    MessageID = fullMsg.Key.Id,
                    StatusJidList = options.StatusJidList,
                });

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
            var destinationJud = isStatus ? statusId : JidEncode(jidDecoded.User, isLid ? "lid" : isGroup ? "g.us" : "s.whatsapp.net");
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

            }
            else
            {
                var meDecode = JidDecode(meId);
                if (options.Participant == null)
                {
                    devices.Add(new JidWidhDevice() { User = jidDecoded.User });
                    // do not send message to self if the device is 0 (mobile)
                    if (meDecode.Device != 0)
                    {
                        devices.Add(new JidWidhDevice() { User = meDecode.User });
                    }

                    var additionalDevices = await GetUSyncDevices([meId, jid], options.UseUserDevicesCache ?? false, true);
                }

            }


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
                            {"mode","usync" },
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

            var json = JsonConvert.SerializeObject(iq,Formatting.Indented);

            var result = await Query(iq);


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
