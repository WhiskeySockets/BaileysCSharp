using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using static WhatsSocket.Core.Models.ChatConstants;
using static WhatsSocket.Core.WABinary.Constants;

namespace WhatsSocket.Core
{
    public abstract class ChatSocket : BaseSocket
    {




        public ChatSocket([NotNull] SocketConfig config) : base(config)
        {
            events["CB:presence"] = HandlePresenceUpdate;
            events["CB:chatstate"] = HandlePresenceUpdate;
            events["CB:ib,,dirty"] = HandleDirtyUpdate;

            var connectionUpdateEvent = EV.On<ConnectionState>(EmitType.Update);
            connectionUpdateEvent.Emit += ConnectionUpdateEvent_Emit;
        }

        private void ConnectionUpdateEvent_Emit(BaseSocket sender, ConnectionState[] args)
        {
            var arg = args[0];
            if (arg.Connection == WAConnectionState.Open)
            {
                if (SocketConfig.FireInitQueries)
                {
                    ///TODO:
                }

                SendPresenceUpdate(SocketConfig.MarkOnlineOnConnect ? WAPresence.Available : WAPresence.Unavailable);

            }
            if (arg.ReceivedPendingNotifications)
            {
                // if we don't have the app state key
                // we keep buffering events until we finally have
                // the key and can sync the messages
                if (Creds?.MyAppStateKeyId != null && !SocketConfig.Mobile)
                {
                    EV.Buffer();
                    NeedToFlushWithAppStateSync = true;
                }
            }
        }

        private void SendPresenceUpdate(WAPresence type, string toJid = "")
        {
            var me = Creds.Me;
            if (type == WAPresence.Available || type == WAPresence.Unavailable)
            {
                if (me?.Name == null)
                {
                    Logger.Warn("no name present, ignoring presence update requests...");
                    return;
                }
                EV.Emit(EmitType.Update, new ConnectionState() { IsOnline = type == WAPresence.Available });

                SendNode(new BinaryNode()
                {
                    tag = "presence",
                    attrs =
                    {
                        { "name", me.Name },
                        { "type" , type.ToString().ToLowerInvariant() }
                    }
                });
            }
            else
            {
                var childNode = new BinaryNode()
                {
                    tag = type == WAPresence.Recording ? "composing" : type.ToString().ToLowerInvariant(),
                };
                if (type == WAPresence.Recording)
                {
                    childNode = new BinaryNode()
                    {
                        tag = type == WAPresence.Recording ? "composing" : type.ToString().ToLowerInvariant(),
                        attrs = {
                            {"media" ,"audio" }
                        }
                    };
                }
                SendNode(new BinaryNode()
                {
                    tag = "chatstate",
                    attrs =
                    {
                        {"from", me.ID },
                        {"to", toJid}
                    },
                    content = new BinaryNode[] { childNode }
                });

            }
        }

        private bool PendingAppStateSync { get; set; } = false;
        private bool NeedToFlushWithAppStateSync { get; set; } = false;

        private async Task<bool> HandlePresenceUpdate(BinaryNode node)
        {
            return true;
        }

        protected virtual async Task<bool> HandleDirtyUpdate(BinaryNode node)
        {
            return true;
        }


        #region chats

        protected async Task UpsertMessage(WebMessageInfo msg, MessageUpsertType type)
        {
            EV.Emit(EmitType.Upsert, new MessageUpsertModel(type, msg));
            //EV.MessageUpsert([msg], type);
            if (!string.IsNullOrWhiteSpace(msg.PushName))
            {
                var jid = msg.Key.FromMe ? Creds.Me.ID : (msg.Key.Participant ?? msg.Key.RemoteJid);
                jid = JidUtils.JidNormalizedUser(jid);

                if (!msg.Key.FromMe)
                {
                    EV.Emit(EmitType.Update, [new ContactModel()
                    {
                        ID = jid,
                        Notify = msg.PushName,
                        VerifiedName = msg.VerifiedBizName
                    }]);
                    //EV.ContactUpdated();
                }

                if (msg.Key.FromMe && !string.IsNullOrEmpty(msg.PushName) && Creds.Me.Name != msg.PushName)
                {
                    Creds.Me.Name = msg.PushName;
                    EV.Emit(EmitType.Update, Creds);
                    //EV.CredsUpdate(Creds);
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


            var t1 = new Task(async () =>
            {
                if (historyMsg != null && Creds.MyAppStateKeyId != null)
                {
                    PendingAppStateSync = false;
                    await DoAppStateSync();
                }
            });

            var t2 = new Task(async () =>
            {
                await ProcessMessageUtil.ProcessMessage(msg, shouldProcessHistoryMsg, Creds, Keys, EV);
            });
            t1.Start();
            t2.Start();
            Task.WaitAll(t1, t2);

            if (msg?.Message?.ProtocolMessage?.AppStateSyncKeyShare != null && PendingAppStateSync)
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
                EV.Emit(EmitType.Update, Creds);

                if (NeedToFlushWithAppStateSync)
                {
                    Logger.Debug("Flussing with app state sync");
                    EV.Flush();
                }
            }

        }

        protected async Task ResyncAppState(string[] collections, bool isInitialSync)
        {

            Dictionary<string, ulong> initialVersionMap = new Dictionary<string, ulong>();
            var collectionsToHandle = collections.ToList();

            Dictionary<string, int> attemptsMap = new Dictionary<string, int>();

            ChatMutationMap globalMutationMap = new ChatMutationMap();
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
                        var state = Keys.Get<AppStateSyncVersion>(name);
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
                            var decodedSnapshot = DecodeSyncdSnapshot(name, item.Snapshot, Keys, initialVersionMap[name], Logger, SocketConfig.AppStateMacVerification.Snapshot);

                            var newState = decodedSnapshot.state;
                            states[name] = newState;
                            Logger.Info($"restored state of {name} from snapshot to v{newState.Version} with mutations");
                            globalMutationMap.Assign(decodedSnapshot.mutationMap);
                            Keys.Set<AppStateSyncVersion>(name, newState);
                            //Repository.Storage.AppStateSyncVersionStore.Set(name, newState);
                        }


                        // only process if there are syncd patches
                        if (patches.Count > 0)
                        {
                            var decodePatches = await DecodePatches(name, patches, states[name], Keys, initialVersionMap[name], Logger, SocketConfig.AppStateMacVerification.Patch);

                            Keys.Set<AppStateSyncVersion>(name, decodePatches.state);
                            //Repository.Storage.AppStateSyncVersionStore.Set(name, decodePatches.state);


                            Logger.Info($"synced {name} to v{decodePatches.state.Version}");
                            initialVersionMap[name] = decodePatches.state.Version;
                            globalMutationMap.Assign(decodePatches.mutationMap);
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
                    Keys.Set<AppStateSyncVersion>(lastName, null);
                    //Repository.Storage.AppStateSyncVersionStore.Set(lastName, null);
                    if (isIrrecoverableError)
                    {
                        collectionsToHandle.Remove(lastName);
                    }
                }
            }


            var onMutation = NewAppStateChunkHandler(isInitialSync);
            foreach (var key in globalMutationMap)
            {
                onMutation(globalMutationMap[key]);
            }
        }

        private Action<ChatMutation> NewAppStateChunkHandler(bool isInitialSync)
        {
            return new Action<ChatMutation>((syncAction) =>
            {
                ProcessSyncAction(syncAction, EV, Creds, isInitialSync ? Creds.AccountSettings : null, Logger);
            });
        }

        private bool ShouldSyncHistoryMessage(Message.Types.HistorySyncNotification historyMsg)
        {
            return true;
        }

        internal async Task CleanDirtyBits(string type)
        {
            Logger.Info(new { DateTime.Now }, "clean dirty bits " + type);
            SendNode(new BinaryNode(type)
            {
                tag = "iq",
                attrs =
                {
                    {"to",S_WHATSAPP_NET },
                    {"type" ,"set" },
                    {"xmlns", "urn:xmpp:whatsapp:dirty"  },
                    {"id", GenerateMessageTag() },
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "clean",
                        attrs = 
                        {
                            {"type", type }
                        }
                    }
                }
            });
        }

        #endregion
    }
}
