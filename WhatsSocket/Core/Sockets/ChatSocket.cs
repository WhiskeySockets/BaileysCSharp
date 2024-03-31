using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using WhatsSocket.Exceptions;
using static WhatsSocket.Core.Utils.ChatUtils;
using static WhatsSocket.Core.Models.ChatConstants;
using static WhatsSocket.Core.WABinary.Constants;
using static WhatsSocket.Core.Utils.GenericUtils;
using WhatsSocket.Core.Sockets;
using WhatsSocket.Core.Events;

namespace WhatsSocket.Core
{
    public abstract class ChatSocket : BaseSocket
    {

        protected ProcessingMutex processingMutex;


        public ChatSocket([NotNull] SocketConfig config) : base(config)
        {
            processingMutex = new ProcessingMutex();
            events["CB:presence"] = HandlePresenceUpdate;
            events["CB:chatstate"] = HandlePresenceUpdate;
            events["CB:ib,,dirty"] = HandleDirtyUpdate;

            var connectionUpdateEvent = EV.On<ConnectionState>(EmitType.Update);
            connectionUpdateEvent.Multi += ConnectionUpdateEvent_Emit;
        }

        private void ConnectionUpdateEvent_Emit(ConnectionState[] args)
        {
            var arg = args[0];
            if (arg.Connection == WAConnectionState.Open)
            {
                if (SocketConfig.FireInitQueries)
                {
                    ExecuteInitQueries();
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

        protected virtual async Task<bool> HandleDirtyUpdate(BinaryNode node)
        {
            await Task.Yield();
            var dirtyNode = GetBinaryNodeChild(node, "dirty");
            var type = dirtyNode?.getattr("type");
            switch (type)
            {
                case "account_sync":
                    var lastAccountTypeSync = Creds.LastAccountTypeSync;
                    if (lastAccountTypeSync != null)
                    {
                        await CleanDirtyBits("account_sync", lastAccountTypeSync);
                    }
                    break;
                case "groups":
                    //Will be handled inside groups
                    return false;
                default:
                    Logger.Info(new { node }, $"received unknown sync '{type}'");
                    break;
            }
            return true;
        }


        #region chats

        protected async Task UpsertMessage(WebMessageInfo msg, MessageUpsertType type)
        {
            EV.Emit(EmitType.Upsert, new MessageUpsertModel(type, msg));
            //EV.MessageUpsert([msg], type);
            if (!string.IsNullOrWhiteSpace(msg.PushName))
            {
                var jid = msg.Key.FromMe ? Creds.Me.ID : (msg.Key.Participant != "" ? msg.Key.Participant : msg.Key.RemoteJid);
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

        internal async Task CleanDirtyBits(string type, ulong? fromTimestamp = null)
        {
            Logger.Info(new { DateTime.Now }, "clean dirty bits " + type);

            var attrs = new Dictionary<string, string>()
            {
                {"type", type }
            };
            if (fromTimestamp != null)
            {
                attrs["timestamp"] = $"{fromTimestamp}";
            }

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
                        attrs = attrs
                    }
                }
            });
        }


        public async Task PrivacyQuery(string name, string value)
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"xmlns","privacy" },
                    {"to",S_WHATSAPP_NET },
                    {"type" ,"set" },
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "category",
                        attrs =  { { name,value.ToLower() } }
                    }
                }
            };
            var result = await Query(node);
        }


        public async void UpdateLastSeenPrivacy(WAPrivacyValue value)
        {
            await PrivacyQuery("last", value.ToString());
        }

        public async void UpdateOnlinePrivacy(WAPrivacyOnlineValue value)
        {
            await PrivacyQuery("online", value.ToString());
        }

        public async void UpdateProfilePicturePrivacy(WAPrivacyValue value)
        {
            await PrivacyQuery("profile", value.ToString());
        }

        public async void UpdateStatusPrivacy(WAPrivacyValue value)
        {
            await PrivacyQuery("status", value.ToString());
        }

        public async void UpdateReadReceiptsPrivacy(WAReadReceiptsValue value)
        {
            await PrivacyQuery("readreceipts", value.ToString());
        }

        public async void UpdateGroupsAddPrivacy(WAPrivacyValue value)
        {
            await PrivacyQuery("groupadd", value.ToString());
        }

        public async void UpdateDefaultDisappearingMode(ulong duration)
        {
            await Query(new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"xmlns", "disappearing_mode" },
                    {"to",S_WHATSAPP_NET },
                    {"type","set" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "disappearing_mode",
                        attrs =
                        {
                            {"duration",$"{duration}" }
                        }
                    }
                }
            });
        }


        //TODO: THIS IS NOT WORKING
        public async Task<BinaryNode[]> InteractiveQuery(BinaryNode[] userNodes, BinaryNode queryNode)
        {
            var result = await Query(new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to",S_WHATSAPP_NET },
                    {"type","get" },
                    {"xmlns","usync" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "usync",
                        attrs =
                        {
                            {"sid",GenerateMessageTag() },
                            {"mode","query" },
                            {"last","true" },
                            {"index","0" },
                            {"context","interactive" }
                        },
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "query",
                                content = new BinaryNode[]{queryNode}
                            },
                            new BinaryNode()
                            {
                                tag = "list",
                                content = userNodes
                            }
                        }
                    }
                }
            });

            var usyncNode = GetBinaryNodeChild(result, "usync");
            var listNode = GetBinaryNodeChild(usyncNode, "list");
            var users = GetBinaryNodeChildren(listNode, "users");
            return users;
        }


        public async Task<OnWhatsAppResult[]> OnWhatsApp(params string[] jids)
        {
            var query = new BinaryNode()
            {
                tag = "contact",
                attrs = { }
            };
            var list = jids.Select(x => new BinaryNode()
            {
                tag = "user",
                attrs = { },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "contact",
                        attrs = {},
			            // insures only 1 + is there
                        content = Encoding.UTF8.GetBytes( $"+{x.Replace("+","")}")
                    }
                }
            }).ToArray();

            var result = await InteractiveQuery(list, query);

            return result.Select(x => new OnWhatsAppResult(x)).ToArray();
        }


        public async Task<StatusResult> FetchStatus(string jid)
        {
            var results = await InteractiveQuery(
                [new BinaryNode()
                {
                    tag = "user",
                    attrs = { {"jid",jid} }
                }
                ], new BinaryNode()
                {
                    tag = "status",
                    attrs = { }
                });
            if (results.Length > 0)
            {
                var result = results[0];
                var status = GetBinaryNodeChild(result, "status");
                return new StatusResult()
                {
                    SetAt = Convert.ToUInt64(status.getattr("t") ?? "0"),
                    Status = Encoding.UTF8.GetString(status.ToByteArray())
                };
            }
            return new StatusResult();
        }

        //TODO updateProfilePicture
        //TODO removeProfilePicture
        //TODO updateProfileStatus
        //TODO updateProfileName
        private async void FetchBlocklist()
        {
            var fetchBlocklist = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"xmlns", "blocklist" },
                    {"to", S_WHATSAPP_NET },
                    {"type", "get" }
                },
            };

            var result = await Query(fetchBlocklist);
            var listNode = GetBinaryNodeChild(result, "list");
            var binary = GetBinaryNodeChildren(listNode, "item");
            var count = binary.Count();

        }
        //TODO updateBlockStatus
        //TODO getBusinessProfile
        //TODO getBusinessProfile
        //TODO profilePictureUrl
        //TODO presenceSubscribe
        //TODO presenceSubscribe

        private async Task<bool> HandlePresenceUpdate(BinaryNode node)
        {
            await Task.Yield();
            PresenceData presence = null;
            var jid = node.getattr("from");
            var participant = node.getattr("participant") ?? jid;
            var tag = node.tag;

            if (SocketConfig.ShouldIgnoreJid(jid))
            {
                return false;
            }

            if (tag == "presence")
            {
                presence = new PresenceData()
                {
                    LastKnownPresence = node.getattr("type") == "unavailable" ? WAPresence.Unavailable : WAPresence.Available,
                    LastSeen = node.getattr("last") != "deny" ? Convert.ToUInt32(node.getattr("last")) : null
                };
            }
            else if (node.content is BinaryNode[] children)
            {
                var firstChild = children[0];
                var type = PresenceModel.Map(firstChild.tag);
                if (type == WAPresence.Paused)
                {
                    type = WAPresence.Available;
                }

                if (firstChild.getattr("media") == "audio")
                {
                    type = WAPresence.Recording;
                }
                presence = new PresenceData()
                {
                    LastKnownPresence = type
                };
            }

            if (presence != null)
            {
                EV.Emit(EmitType.Update, new PresenceModel()
                {
                    ID = jid,
                    Presences =
                    {
                        { participant, presence }
                    }
                });
            }
            return true;
        }


        //TODO: appPatch
        private async void FetchAbt()
        {
            var abtNode = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to", S_WHATSAPP_NET },
                    {"xmlns", "abt" },
                    {"type", "get" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "props",
                        attrs =
                        {
                            {"protocol","1" }
                        }
                    }
                }

            };
            abtNode = await Query(abtNode);
            var propsNode = GetBinaryNodeChild(abtNode, "props");

            if (propsNode != null)
            {
                var props = ReduceBinaryNodeToDictionary(propsNode, "prop");
            }
        }

        Dictionary<string, string> privacySettings;
        private async void FetchPrivacySettings()
        {
            if (privacySettings == null)
            {
                var fetchPrivacySettings = new BinaryNode()
                {
                    tag = "iq",
                    attrs =
                {
                    {"xmlns", "privacy" },
                    {"to", S_WHATSAPP_NET },
                    {"type", "get" }
                },
                    content = new BinaryNode[]
                    {
                    new BinaryNode()
                    {
                        tag = "privacy",
                        attrs ={ }

                    }
                    }

                };
                var result = await Query(fetchPrivacySettings);
                result = (result.content as BinaryNode[])[0];
                privacySettings = ReduceBinaryNodeToDictionary(result, "category");
            }
        }

        private async void FetchProps()
        {
            var fetchProps = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to", S_WHATSAPP_NET },
                    {"xmlns", "w" },
                    {"type", "get" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "props",
                        attrs ={ }

                    }
                }

            };
            var resultNode = await Query(fetchProps);
            var propsNode = GetBinaryNodeChild(resultNode, "props");

            if (propsNode != null)
            {
                var props = ReduceBinaryNodeToDictionary(propsNode, "prop");
            }
        }

        //TODO: chatModify
        //TODO: star
        //TODO: addChatLabel
        //TODO: removeChatLabel
        //TODO: addMessageLabel
        //TODO: removeMessageLabel

        private void ExecuteInitQueries()
        {
            FetchAbt();
            FetchProps();
            FetchBlocklist();
            FetchPrivacySettings();
        }







        #endregion
    }
}
