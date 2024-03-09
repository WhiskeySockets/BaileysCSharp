using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;
using static Proto.Message.Types;

namespace WhatsSocket.Core.Utils
{
    public class HistoryUtil
    {
        public static HistorySyncNotification GetHistoryMsg(Message message)
        {
            var normalizedContent = message != null ? MessageUtil.NormalizeMessageContent(message) : null;
            var anyHistoryMsg = normalizedContent?.ProtocolMessage?.HistorySyncNotification;
            return anyHistoryMsg;

        }


        public static async Task<object> DownloadAndProcessHistorySyncNotification(Message.Types.HistorySyncNotification msg)
        {
            var historyMsg = await DownloadHistory(msg);
            return ProcessHistoryMessage(historyMsg);
        }

        private static object ProcessHistoryMessage(HistorySync item)
        {
            List<Contact> contacts = new List<Contact>();

            switch (item.SyncType)
            {
                case HistorySync.Types.HistorySyncType.InitialBootstrap:
                case HistorySync.Types.HistorySyncType.Recent:
                case HistorySync.Types.HistorySyncType.Full:

                    foreach (var chat in item.Conversations)
                    {
                        contacts.Add(new Contact()
                        {
                            ID = chat.Id,
                            Name = chat.Name,
                        });

                    }

                    break;


                case HistorySync.Types.HistorySyncType.InitialStatusV3:
                    break;
                case HistorySync.Types.HistorySyncType.PushName:
                    break;
                case HistorySync.Types.HistorySyncType.NonBlockingData:
                    break;
                case HistorySync.Types.HistorySyncType.OnDemand:
                    break;
                default:
                    break;
            }
            return null;
        }

        private static async Task<HistorySync> DownloadHistory(Message.Types.HistorySyncNotification msg)
        {
            var stream = await MediaMessageUtil.DownloadContentFromMessage(msg, "md-msg-hist", new Models.MediaDownloadOptions());
            var buffer = BufferReader.Inflate(stream);
            var syncData = HistorySync.Parser.ParseFrom(buffer);
            return syncData;
        }

    }
}
