using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using static WhatsSocket.Core.Utils.GenericUtils;

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

        private Task HandleNotification(BinaryNode node)
        {
            return Task.CompletedTask;
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
