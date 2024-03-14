using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using static WhatsSocket.Core.Utils.ProcessMessageUtil;

namespace WhatsSocket.Core
{
    public partial class BaseSocket
    {


        #region messages-recv


        private async Task<bool> OnHandleAck(BinaryNode node)
        {
            return await ValidateConnectionUtil.ProcessNodeWithBuffer(node, "handling ack", HandleAck);
        }

        private Task<bool> HandleAck(BinaryNode node)
        {

            return Task.FromResult(true);
        }

        private async Task<bool> OnNotification(BinaryNode node)
        {
            return await ValidateConnectionUtil.ProcessNodeWithBuffer(node, "handling notification", HandleNotification);
        }

        private async Task HandleNotification(BinaryNode node)
        {
            var remoteJid = node.attrs["from"];
            if (SocketConfig.ShouldIgnoreJid(remoteJid))
            {
                Logger.Debug(new { remoteJid, id = node.attrs["id"] }, "ignored notification");
                SendMessageAck(node);
                return;
            }

            var msg = await ProcessNotifciation(node);

            SendMessageAck(node);

        }


        public async Task<WebMessageInfo> ProcessNotifciation(BinaryNode node)
        {
            var result = new WebMessageInfo();
            var child = GetAllBinaryNodeChildren(node).FirstOrDefault();

            var type = node.attrs["type"];

            switch (type)
            {
                case "privacy_token":
                    var tokenList = GetBinaryNodeChildren(child, "token");
                    foreach (var item in tokenList)
                    {
                        var jid = item.getattr("jid");
                        EV.ChatUpdate([new ChatModel()
                        {
                            ID = jid,
                            TcToken = item.content as byte[]
                        }]);

                        Logger.Debug(new { jid }, "got privacy token update");
                    }
                    break;
                case "w:gp2":
                    await HandleGroupNotification(node.attrs["participant"], child, result);
                    break;
                case "mediaretry":
                    break;
                case "encrypt":
                    break;
                case "devices":
                    var devices = GetBinaryNodeChildren(child, "device");
                    if (JidUtils.AreJidsSameUser(child?.attrs["jid"], Creds?.Me.ID))
                    {
                        var deviceJids = devices.Select(x => x.attrs["jid"]).ToArray();
                        Logger.Info(new { deviceJids }, "got my own devices");
                    }
                    break;
                case "server_sync":
                    var update = GetBinaryNodeChild(node, "collection");
                    if (update != null)
                    {
                        var name = update.attrs["name"];
                        await ResyncAppState([name], false);
                    }
                    break;
                case "picture":
                    break;
                case "account_sync":
                    break;
                case "link_code_companion_reg":
                    break;

                default:
                    break;
            }

            return null;
        }


        private async Task HandleGroupNotification(string participant, BinaryNode child, WebMessageInfo msg)
        {
            switch (child.tag)
            {
                case "create":
                    var metadata = ExtractGroupMetaData(child);
                    msg.MessageStubType = WebMessageInfo.Types.StubType.GroupCreate;
                    msg.MessageStubParameters.Add(metadata.Subject);
                    metadata.Author = participant;
                    msg.Key = new MessageKey()
                    {
                        Participant = metadata.Owner,
                    };
                    EV.ChatUpsert([new ChatModel()
                    {
                        ID = metadata.ID,
                        Name = metadata.Subject ?? "",
                        ConversationTimestamp = metadata.Creation
                    }]);

                    EV.GroupInsert([metadata]);
                    break;
                case "ephemeral":
                case "not_ephemeral":
                    msg.Message = new Message()
                    {
                        ProtocolMessage = new Message.Types.ProtocolMessage()
                        {
                            Type = Message.Types.ProtocolMessage.Types.Type.EphemeralSetting,
                            EphemeralExpiration = child.getattr("expiration").ToUInt32(),
                        }
                    };
                    break;
                case "promote":
                case "demote":
                case "remove":
                case "add":
                case "leave":
                    Dictionary<string, WebMessageInfo.Types.StubType> WAMessageStubType = new Dictionary<string, WebMessageInfo.Types.StubType>();
                    foreach (WebMessageInfo.Types.StubType value in Enum.GetValues(typeof(WebMessageInfo.Types.StubType)))
                    {
                        WAMessageStubType.Add(value.ToString(), value);
                    }
                    var stubType = $"GROUPPARTICIPANT{child.tag.ToUpper()}";
                    msg.MessageStubType = WAMessageStubType[stubType];
                    var participants = GetBinaryNodeChildren(child, "participant").Select(p => p.attrs["jid"]).ToArray();
                    if (participants.Length == 1 && JidUtils.AreJidsSameUser(participants[0], participant) && child.tag == "remove")
                    {
                        msg.MessageStubType = WebMessageInfo.Types.StubType.GroupParticipantLeave;
                    }
                    msg.MessageStubParameters.AddRange(participants);
                    break;
                case "subject":
                    msg.MessageStubType = WebMessageInfo.Types.StubType.GroupChangeSubject;
                    msg.MessageStubParameters.Add(child.attrs["subject"]);
                    break;
                case "announcement":
                case "not_announcement":
                    msg.MessageStubType = WebMessageInfo.Types.StubType.GroupChangeAnnounce;
                    msg.MessageStubParameters.Add((child.tag == "announcement") ? "on" : "off");
                    break;
                case "locked":
                case "unlocked":
                    msg.MessageStubType = WebMessageInfo.Types.StubType.GroupChangeRestrict;
                    msg.MessageStubParameters.Add((child.tag == "locked") ? "on" : "off");
                    break;
                case "invite":
                    msg.MessageStubType = WebMessageInfo.Types.StubType.GroupChangeInviteLink;
                    msg.MessageStubParameters.Add(child.attrs["code"]);
                    break;
                case "member_add_mode":
                    var addMode = child.content as byte[];
                    if (addMode != null)
                    {
                        msg.MessageStubType = WebMessageInfo.Types.StubType.GroupMemberAddMode;
                        msg.MessageStubParameters.Add(Encoding.UTF8.GetString(addMode));
                    }
                    break;
                case "membership_approval_mode":
                    var approvalMode = GetBinaryNodeChild(child, "group_join");
                    if (approvalMode != null)
                    {
                        msg.MessageStubType = WebMessageInfo.Types.StubType.GroupMembershipJoinApprovalMode;
                        msg.MessageStubParameters.Add(approvalMode.attrs["state"]);

                    }
                    break;
            }
        }

        private static GroupMetadataModel ExtractGroupMetaData(BinaryNode result)
        {
            var group = GetBinaryNodeChild(result, "group");
            var descChild = GetBinaryNodeChild(result, "description");
            string desc = "";
            string descId = "";
            if (descChild != null)
            {
                desc = GetBinaryNodeChildString(descChild, "body");
                descId = descChild.attrs["id"];
            }


            var groupId = group.attrs["id"].Contains("@") ? group.attrs["id"] : JidUtils.JidEncode(group.attrs["id"], "g.us");
            var eph = GetBinaryNodeChild(group, "ephemeral")?.attrs["expiration"].ToUInt64();

            var participants = GetBinaryNodeChildren(group, "participant");
            var memberAddMode = GetBinaryNodeChildString(group, "member_add_mode") == "all_member_add";

            var metadata = new GroupMetadataModel
            {
                ID = groupId,
                Subject = group.getattr("subject"),
                SubjectOwner = group.getattr("s_o"),
                SubjectTime = group.getattr("s_t").ToUInt64(),
                Size = (ulong)participants.Length,
                Creation = group.attrs["creation"].ToUInt64(),
                Owner = group.getattr("creator") != null ? JidUtils.JidNormalizedUser(group.attrs["creator"]) : null,
                Desc = desc,
                DescID = descId,
                Restrict = GetBinaryNodeChild(group, "locked") != null,
                Announce = GetBinaryNodeChild(group, "announcement") != null,
                IsCommunity = GetBinaryNodeChild(group, "parent") != null,
                IsCommunityAnnounce = GetBinaryNodeChild(group, "default_sub_group") != null,
                JoinApprovalMode = GetBinaryNodeChild(group, "membership_approval_mode") != null,
                MemberAddMode = memberAddMode,
                Participants = participants.Select(x => new GroupParticipantModel()
                {
                    ID = x.attrs["jid"],
                    ParticipantType = x.getattr("type")

                }).ToArray(),
                EphemeralDuration = eph
            };

            return metadata;
        }

        private async Task<bool> OnReceipt(BinaryNode node)
        {
            return await ValidateConnectionUtil.ProcessNodeWithBuffer(node, "handling receipt", HandleReceipt);
        }

        private Task HandleReceipt(BinaryNode node)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> OnCall(BinaryNode node)
        {
            return await ValidateConnectionUtil.ProcessNodeWithBuffer(node, "handling call", HandleCall);
        }

        private Task HandleCall(BinaryNode node)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> OnMessage(BinaryNode node)
        {
            return await ValidateConnectionUtil.ProcessNodeWithBuffer(node, "processing message", HandleMessage);
        }
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private async Task HandleMessage(BinaryNode node)
        {
            await semaphoreSlim.WaitAsync();

            var result = MessageDecoder.DecryptMessageNode(node, Creds.Me.ID, Creds.Me.LID, Repository, Logger);
            result.Decrypt();

            if (result.Msg.MessageStubType == WebMessageInfo.Types.StubType.Ciphertext)
            {
                var encNode = GetBinaryNodeChild(node, "enc");
                if (encNode != null)
                {
                    SendRetryRequest(node, encNode != null);
                }

            }
            else
            {
                // no type in the receipt => message delivered
                var type = MessageReceiptType.Undefined;
                var participant = result.Msg.Key.Participant;
                if (result.Category == "peer") // special peer message
                {
                    type = MessageReceiptType.PeerMsg;
                }
                else if (result.Msg.Key.FromMe) // message was sent by us from a different device
                {
                    type = MessageReceiptType.Sender;
                    if (JidUtils.IsJidUser(result.Author))
                    {
                        participant = result.Author;
                    }
                    // need to specially handle this case
                }
                else if (!SendActiveReceipts)
                {
                    type = MessageReceiptType.Inactive;
                }

                //When upsert works this is to be implemented
                //SendReceipt(result.Msg.Key.RemoteJid, participant, type, result.Msg.Key.Id);

                var isAnyHistoryMsg = HistoryUtil.GetHistoryMsg(result.Msg.Message);
                if (isAnyHistoryMsg != null)
                {
                    var jid = JidUtils.JidNormalizedUser(result.Msg.Key.RemoteJid);
                    SendReceipt(jid, null, MessageReceiptType.HistSync, result.Msg.Key.Id);
                }
            }

            ProcessMessageUtil.CleanMessage(result.Msg, Creds.Me.ID);


            await UpsertMessage(result.Msg, node.getattr("offline") != null ? "append" : "notify");

            //When upsert works this is to be implemented
            SendMessageAck(node);
            semaphoreSlim.Release();
        }


        private void SendMessageAck(BinaryNode node)
        {
            var stanza = new BinaryNode("ack")
            {
                attrs = new Dictionary<string, string>()
                    {
                        {"id", node.attrs["id"] },
                        {"to", node.attrs["from"] },
                        {"class", node.tag },
                    },
            };
            if (node.attrs.ContainsKey("participant"))
            {
                stanza.attrs["participant"] = node.attrs["participant"];
            }
            if (node.attrs.ContainsKey("recipient"))
            {
                stanza.attrs["recipient"] = node.attrs["recipient"];
            }
            if (node.tag != "message")
            {
                stanza.attrs["type"] = node.attrs["type"];
            }
            Logger.Debug(new { recv = new { node.tag, node.attrs }, sent = stanza.attrs }, "sent ack");
            SendNode(stanza);
        }

        private void SendReceipt(string jid, string participant, string type, params string[] messageIds)
        {

            var node = new BinaryNode("receipt")
            {
                attrs = new Dictionary<string, string>()
                    {
                        {"id", messageIds[0] },
                    },
            };
            if (type == MessageReceiptType.Read || type == MessageReceiptType.ReadSelf)
            {
                node.attrs["t"] = DateTime.Now.UnixTimestampSeconds().ToString();
            }
            if (type == MessageReceiptType.Sender && JidUtils.IsJidUser(jid))
            {
                node.attrs["recipient"] = jid;
                if (!string.IsNullOrWhiteSpace(participant))
                {
                    node.attrs["to"] = participant;
                }
            }
            else
            {
                node.attrs["to"] = jid;
                if (!string.IsNullOrWhiteSpace(participant))
                {
                    node.attrs["recipient"] = participant;
                }
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                node.attrs["type"] = type;
            }
            var remaining = messageIds.Skip(1).ToArray();
            if (remaining.Length > 0)
            {
                node.content = new BinaryNode("list")
                {
                    content = remaining.Select(x => new BinaryNode("item")
                    {
                        attrs = new Dictionary<string, string>()
                        {
                            { "id", x }
                        }
                    })
                };
            }
            Logger.Info(new { node.attrs, messageIds }, "sending receipt for messages");
            SendNode(node);
        }

        private void SendRetryRequest(BinaryNode node, bool forceIncludeKeys = false)
        {
            var msgId = node.attrs["id"];

            //Check Retries
            if (!MessageRetries.ContainsKey(msgId))
            {
                MessageRetries.Add(msgId, 0);
            }
            var retryCount = MessageRetries[msgId];
            if (retryCount > 5)
            {
                MessageRetries.Remove(msgId);
                return;
            }
            retryCount++;
            MessageRetries[msgId] = retryCount;

            //var deviceIdentity = SocketHelper.EncodeSignedDeviceIdentity(Creds, true);
        }


        #endregion

    }
}
