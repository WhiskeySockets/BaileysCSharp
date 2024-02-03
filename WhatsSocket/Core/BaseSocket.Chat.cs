using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Utils;

namespace WhatsSocket.Core
{
    public partial class BaseSocket
    {

        #region chats

        private void UpsertMessage(WebMessageInfo msg, string v)
        {
            if (!string.IsNullOrWhiteSpace(msg.PushName))
            {
                var jid = msg.Key.FromMe ? Creds.Me.ID : (msg.Key.Participant ?? msg.Key.RemoteJid);
                jid = JidUtils.JidNormalizedUser(jid);

                if (!msg.Key.FromMe)
                {
                    //Event Contact
                }

                if (msg.Key.FromMe && !string.IsNullOrEmpty(msg.PushName) && Creds.Me.Name != msg.PushName)
                {
                    Creds.Me.Name = msg.PushName;
                    this.OnCredentialsChange.Invoke(this, Creds);
                }

            }

            var historyMsg = History.GetHistoryMsg(msg.Message);
            var shouldProcessHistoryMsg = historyMsg != null ?
                (ShouldSyncHistoryMessage(historyMsg)
                & Constants.PROCESSABLE_HISTORY_TYPES.Contains(historyMsg.SyncType))
                : false;

            var pendingAppStateSync = false;
            if (historyMsg != null && Creds.MyAppStateKeyId == null)
            {
                Logger.Warn("skipping app state sync, as myAppStateKeyId is not set");
                pendingAppStateSync = true;
            }

            if (historyMsg != null && Creds.MyAppStateKeyId != null)
            {
                pendingAppStateSync = false;
                DoAppStateSync();
            }

            ProcessMessageUtil.ProcessMessage(msg, shouldProcessHistoryMsg, Creds, Keys);
        }

        private void DoAppStateSync()
        {
        }

        private bool ShouldSyncHistoryMessage(Message.Types.HistorySyncNotification historyMsg)
        {
            return true;
        }

        #endregion
    }
}
