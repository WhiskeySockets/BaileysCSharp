using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


        public static object DownloadAndProcessHistorySyncNotification(Message.Types.HistorySyncNotification msg)
        {
            var historyMsg = DownloadHistory(msg);
            return ProcessHistoryMessage(historyMsg);
        }

        private static object ProcessHistoryMessage(object msg)
        {
            return null;
        }

        private static object DownloadHistory(Message.Types.HistorySyncNotification msg)
        {
            return null;
        }

    }
}
