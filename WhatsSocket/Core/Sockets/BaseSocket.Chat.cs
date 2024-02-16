using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using static WhatsSocket.Core.Utils.ChatUtils;

namespace WhatsSocket.Core
{
    public partial class BaseSocket
    {

        public static string[] ALL_WA_PATCH_NAMES = ["critical_block", "critical_unblock_low", "regular_high", "regular_low", "regular"];

        private bool NeedToFlushWithAppStateSync { get; set; } = false;


        #region chats

        private async Task UpsertMessage(WebMessageInfo msg, string v)
        {
            if (!string.IsNullOrWhiteSpace(msg.PushName))
            {
                var jid = msg.Key.FromMe ? Creds.Me.ID : (msg.Key.Participant ?? msg.Key.RemoteJid);
                jid = JidUtils.JidNormalizedUser(jid);

                if (!msg.Key.FromMe)
                {
                    EV.Emit(new Contact()
                    {
                        ID = jid,
                        Notify = msg.PushName,
                        VerifiedName = msg.VerifiedBizName
                    });
                }

                if (msg.Key.FromMe && !string.IsNullOrEmpty(msg.PushName) && Creds.Me.Name != msg.PushName)
                {
                    Creds.Me.Name = msg.PushName;
                    EV.Emit(Creds);
                }

            }

            var historyMsg = HistoryUtil.GetHistoryMsg(msg.Message);
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
                await DoAppStateSync();
            }

            ProcessMessageUtil.ProcessMessage(msg, shouldProcessHistoryMsg, Creds, Repository, EV);
        }

        private async Task DoAppStateSync()
        {
            if (Creds.AccountSyncCounter == 0)
            {
                Logger.Info("Doing initial app state sync");
                await ResyncAppState(ALL_WA_PATCH_NAMES, true);

                Creds.AccountSyncCounter++;
                EV.Emit(Creds);
            }

            if (NeedToFlushWithAppStateSync)
            {
                Logger.Debug("Flussing with app state sync");
                //Unsure if I am going to follow the buffer pattern if we are going to make use of a NoSQL or SQLLite approach
            }
        }

        private async Task ResyncAppState(string[] collections, bool isInitialSync)
        {
            Dictionary<string, int> initialVersionMap = new Dictionary<string, int>();
            var collectionsToHandle = collections.ToList();

            while (collectionsToHandle.Count > 0)
            {
                List<BinaryNode> nodes = new List<BinaryNode>();
                foreach (var name in collectionsToHandle)
                {
                    var state = Repository.Storage.AppStateSyncVersionStore.Get(name);

                    if (state != null)
                    {
                        initialVersionMap[name] = state.Version;
                    }
                    else
                    {
                        state = new Stores.AppStateSyncVersion();
                    }

                    Logger.Info($"Resyncing {name} from v{state.Version}");

                    nodes.Add(new BinaryNode("collection")
                    {
                        attrs = new Dictionary<string, string> {
                            {
                                name,name
                            },
                            {
                                "version",
                                 state.Version.ToString()
                            },
                            {
                                "resturn_snapspot",
                                (state.Version == 0).ToString() // make sure this match
                            }
                        }
                    });

                    var query = new BinaryNode("iq")
                    {
                        attrs = new Dictionary<string, string>
                        {
                            {"to",Constants.S_WHATSAPP_NET },
                            {"xmlns","w:sync:app:state" },
                            {"type","set" }
                        },
                        content = new BinaryNode[]
                        {
                            new BinaryNode("sync",nodes.ToArray())
                        }
                    };

                    var result = await Query(query);

                    var decoded = await ExtractSyncedPathces(result);
                }
            }
        }


        private bool ShouldSyncHistoryMessage(Message.Types.HistorySyncNotification historyMsg)
        {
            return true;
        }

        #endregion
    }
}
