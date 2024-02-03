using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Proto.Message.Types;

namespace WhatsSocket.Core.Utils
{
    public class History
    {
        public static HistorySyncNotification GetHistoryMsg(Message message)
        {
            var normalizedContent = message == null ? MessageUtil.NormalizeMessageContent(message) : null;
            var anyHistoryMsg = normalizedContent?.ProtocolMessage?.HistorySyncNotification;
            return anyHistoryMsg;

        }
    }
}
