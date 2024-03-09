using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using WhatsSocket.Exceptions;
using static WhatsSocket.Core.Utils.ChatUtils;

namespace WhatsSocket.Core
{
    public partial class BaseSocket
    {

        public static string[] ALL_WA_PATCH_NAMES = ["critical_block", "critical_unblock_low", "regular_high", "regular_low", "regular"];

        private bool PendingAppStateSync { get; set; } = false;
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
                && Constants.PROCESSABLE_HISTORY_TYPES.Contains(historyMsg.SyncType))
                : false;

            if (historyMsg != null && Creds?.MyAppStateKeyId == null)
            {
                Logger.Warn("skipping app state sync, as myAppStateKeyId is not set");
                PendingAppStateSync = true;
            }


            if (historyMsg != null && Creds.MyAppStateKeyId != null)
            {
                PendingAppStateSync = false;
                await DoAppStateSync();
            }

            await ProcessMessageUtil.ProcessMessage(msg, shouldProcessHistoryMsg, Creds, Repository, EV);

            if (msg.Message.ProtocolMessage.AppStateSyncKeyShare != null && PendingAppStateSync)
            {
                await DoAppStateSync();
                PendingAppStateSync = false;
            }
        }

        private async Task DoAppStateSync()
        {
            if (Creds?.AccountSyncCounter == 0)
            {
                Logger.Info("Doing initial app state sync");
                await ResyncAppState(ALL_WA_PATCH_NAMES, true);

                Creds.AccountSyncCounter++;
                EV.Emit(Creds);

                if (NeedToFlushWithAppStateSync)
                {
                    Logger.Debug("Flussing with app state sync");
                    EV.Flush();
                }
            }

        }

        private async Task ResyncAppState(string[] collections, bool isInitialSync)
        {

            Dictionary<string, ulong> initialVersionMap = new Dictionary<string, ulong>();
            var collectionsToHandle = collections.ToList();

            Dictionary<string, int> attemptsMap = new Dictionary<string, int>();

            Dictionary<string, ChatMutation> globalMutationMap = new Dictionary<string, ChatMutation>();
            foreach (var collection in collections)
            {
                attemptsMap[collection] = 0;
                initialVersionMap[collection] = 0;
            }


            while (collectionsToHandle.Count > 0)
            {
                string lastName = "";
                try
                {
                    Dictionary<string, AppStateSyncVersion> states = new Dictionary<string, AppStateSyncVersion>();
                    List<BinaryNode> nodes = new List<BinaryNode>();
                    foreach (var name in collections)
                    {
                        lastName = name;
                        var state = Repository.Storage.AppStateSyncVersionStore.Get(name);
                        if (state != null)
                        {
                            if (!initialVersionMap.ContainsKey(name))
                            {
                                initialVersionMap[name] = state.Version;
                            }
                        }
                        else
                        {
                            state = new Stores.AppStateSyncVersion();
                        }
                        states[name] = state;

                        Logger.Info($"Resyncing {name} from v{state.Version}");

                        nodes.Add(new BinaryNode("collection")
                        {
                            attrs = new Dictionary<string, string> {
                            {
                                "name",name
                            },
                            {
                                "version",
                                 state.Version.ToString()
                            },
                            {
                                "return_snapshot",
                                (state.Version == 0 ? "true": "false") // make sure this match
                            }
                        }
                        });
                    }

                    var query = new BinaryNode()
                    {
                        tag = "iq",
                        attrs = new Dictionary<string, string>
                        {
                            {"to",Constants.S_WHATSAPP_NET },
                            {"xmlns","w:sync:app:state" },
                            {"type","set" }
                        },
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "sync",
                                content = nodes.ToArray()
                            }
                        }
                    };

                    var result = await Query(query);

                    // extract from binary node
                    var decoded = await ExtractSyncedPathces(result);
                    foreach (var keyPair in decoded)
                    {
                        var name = keyPair.Key;
                        lastName = name;
                        var item = keyPair.Value;


                        var patches = decoded[name].Patches;

                        if (item.Snapshot != null)
                        {
                            var decodedSnapshot = DecodeSyncdSnapshot(name, item.Snapshot, Repository.Storage.AppStateSyncKeyStore, initialVersionMap[name], Logger, Config.AppStateMacVerification.Snapshot);

                            var newState = decodedSnapshot.state;
                            states[name] = newState;
                            Logger.Info($"restored state of {name} from snapshot to v{newState.Version} with mutations");
                            foreach (var map in decodedSnapshot.mutationMap)
                            {
                                globalMutationMap[map.Key] = map.Value;
                            }
                            Repository.Storage.AppStateSyncVersionStore.Set(name, newState);
                        }


                        // only process if there are syncd patches
                        if (patches.Count > 0)
                        {
                            var decodePatches = await DecodePatches(name, patches, states[name], Repository.Storage.AppStateSyncKeyStore, initialVersionMap[name], Logger, Config.AppStateMacVerification.Patch);

                            Repository.Storage.AppStateSyncVersionStore.Set(name, decodePatches.state);


                            Logger.Info($"synced {name} to v{decodePatches.state.Version}");
                            initialVersionMap[name] = decodePatches.state.Version;
                            foreach (var map in decodePatches.mutationMap)
                            {
                                globalMutationMap[map.Key] = map.Value;
                            }
                        }

                        if (keyPair.Value.HasMorePatches)
                        {
                            Logger.Info($"{name} has more patches...");
                        }
                        else
                        {
                            collectionsToHandle.Remove(name);
                        }
                    }
                }
                catch (Boom ex)
                {
                    var isIrrecoverableError = ex.Reason == Events.DisconnectReason.NoKeyForMutation || attemptsMap[lastName] < 2;

                    //await authState.keys.set({ 'app-state-sync-version': { [name]: null } })
                    Repository.Storage.AppStateSyncVersionStore.Set(lastName, null);
                    if (isIrrecoverableError)
                    {
                        collectionsToHandle.Remove(lastName);
                    }
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
