using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Core.Utils;
using BaileysCSharp.Core.WABinary;
using BaileysCSharp.Exceptions;
using static BaileysCSharp.Core.Utils.ChatUtils;
using static BaileysCSharp.Core.Models.ChatConstants;
using static BaileysCSharp.Core.WABinary.Constants;
using static BaileysCSharp.Core.Utils.GenericUtils;
using BaileysCSharp.Core.Sockets;
using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core
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


            EV.Connection.Update += Connection_Update;
        }

        private async void Connection_Update(object? sender, ConnectionState e)
        {
            var arg = e;
            if (arg.Connection == WAConnectionState.Open)
            {
                if (SocketConfig.FireInitQueries)
                {
                    await ExecuteInitQueries();
                }

                SendPresenceUpdate(SocketConfig.MarkOnlineOnConnect ? WAPresence.Available : WAPresence.Unavailable);

            }
            if (arg.ReceivedPendingNotifications)
            {
                // if we don't have the app state key
                // we keep buffering events until we finally have
                // the key and can sync the messages
                if (Creds?.MyAppStateKeyId == null && !SocketConfig.Mobile)
                {
                    EV.Buffer();
                    NeedToFlushWithAppStateSync = true;
                }
            }
        }




        private bool PendingAppStateSync { get; set; } = false;
        private bool NeedToFlushWithAppStateSync { get; set; } = false;

        protected virtual async Task<bool> HandleDirtyUpdate(BinaryNode node)
        {
            await Task.Yield();
            var attrs = GetBinaryNodeChild(node, "dirty");
            var type = attrs?.getattr("type");
            switch (type)
            {
                case "account_sync":
                    var lastAccountTypeSync = Creds?.LastAccountTypeSync;
                    if (lastAccountTypeSync != null)
                    {
                        await CleanDirtyBits("account_sync", lastAccountTypeSync);
                    }

                    lastAccountTypeSync = Convert.ToUInt64(attrs.attrs["timestamp"]);
                    if (Creds != null)
                    {
                        Creds.LastAccountTypeSync = lastAccountTypeSync;
                        EV.Emit(EmitType.Update, Creds);
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

        protected async Task UpsertMessage(WebMessageInfo msg, MessageEventType type)
        {
            EV.Emit(EmitType.Upsert, new MessageEventModel(type, msg));
            //EV.MessageUpsert([msg], type);
            if (!string.IsNullOrWhiteSpace(msg.PushName))
            {
                var jid = msg.Key.FromMe ? Creds.Me.ID : (msg.Key.Participant != "" ? msg.Key.Participant : msg.Key.RemoteJid);
                jid = JidUtils.JidNormalizedUser(jid);

                if (!msg.Key.FromMe)
                {
                    var contact = Store.GetContact(jid);
                    if (contact != null)
                    {
                        contact.Notify = msg.PushName;
                        contact.VerifiedName = msg.VerifiedBizName;
                        EV.Emit(EmitType.Update, [contact]);
                    }
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
                await ProcessMessageUtil.ProcessMessage(msg, shouldProcessHistoryMsg, Creds, Keys, Store, EV);
            });
            t1.Start();
            t2.Start();
            Task.WaitAll(t1, t2);
            if (historyMsg?.HasProgress ?? false)
                EV.Emit(EmitType.Update, new SyncState() { Msg = "Sync Data ", Prograss = historyMsg.Progress });

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
                ProcessSyncAction(syncAction, EV, Creds, isInitialSync ? Creds.AccountSettings : null, Store, Logger);
            });
        }

        private bool ShouldSyncHistoryMessage(Message.Types.HistorySyncNotification historyMsg)
        {
            return true;
        }

        public async Task CleanDirtyBits(string type, ulong? fromTimestamp = null)
        {
            await Task.Yield();
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
                    {"to", S_WHATSAPP_NET },
                    {"type" , "set" },
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
            var users = GetBinaryNodeChildren(listNode, "user");
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

        public async Task RemoveProfilePicture(string jid)
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs = {
                    {
                        "to", JidUtils.JidNormalizedUser(jid)
                    } ,
                    {
                        "type", "set"
                    },
                    {
                        "xmlns", "w:profile:picture"
                    }

                }
            };
            await Query(node);
        }

        public async Task UpdateProfileStatus(string status)
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {
                        "to", S_WHATSAPP_NET
                    } ,
                    {
                        "type", "set"
                    },
                    {
                        "xmlns", "status"
                    }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "status",
                        content = Encoding.UTF8.GetBytes(status)
                    }
                }
            };
            await Query(node);
        }

        //TODO updateProfileName
        private async Task FetchBlocklist()
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
        //profilePictureUrl 

        public async Task<string> ProfilePictureUrl(string jid, ProfilePictureUrlType type = ProfilePictureUrlType.Preview)
        {
            jid = JidUtils.JidNormalizedUser(jid);
            var result = await Query(new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    	{"target", jid },
			{"to", S_WHATSAPP_NET },
			{"type", "get" },
			{"xmlns", "w:profile:picture" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "picture",
                        attrs =
                        {
                            {"type", type.ToString().ToLower() },
                            {"query", "url" }
                        }
                    }
                }
            });
            var child = GetBinaryNodeChild(result, "picture");
            //If it is null it means there is no url
            //resulting url is a url pointing to an already decrypted image
            return child?.getattr("url");
        }

        public void SendPresenceUpdate(WAPresence type, string toJid = "")
        {
            var me = Creds?.Me;
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
                        { "name", me?.Name ?? "" },
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
                        {"from", me?.ID ?? "" },
                        {"to", toJid}
                    },
                    content = new BinaryNode[] { childNode }
                });

            }
        }

        public void PresenceSubscribe(string toJid, byte[] tcToken)
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {
                        "to", toJid
                    } ,
                    {
                        "id", GenerateMessageTag()
                    },
                    {
                        "type", "subscribe"
                    }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "tctoken",
                        content = tcToken
                    }
                }
            };
            SendNode(node);
        }

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


        public void AppPatch(WAPatchCreate patch)
        {
            var name = patch.Type;
            var myAppStateKeyId = Creds?.MyAppStateKeyId;
            if (myAppStateKeyId == null)
            {
                throw new Boom("App state key not present!", new BoomData(400));
            }

            //TODO
        }

        //private async Task FetchAbt()
        //{
        //    var abtNode = new BinaryNode()
        //    {
        //        tag = "iq",
        //        attrs =
        //        {
        //            {"to", S_WHATSAPP_NET },
        //            {"xmlns", "abt" },
        //            {"type", "get" }
        //        },
        //        content = new BinaryNode[]
        //        {
        //            new BinaryNode()
        //            {
        //                tag = "props",
        //                attrs =
        //                {
        //                    {"protocol","1" }
        //                }
        //            }
        //        }

        //    };
        //    abtNode = await Query(abtNode);
        //    var propsNode = GetBinaryNodeChild(abtNode, "props");

        //    if (propsNode != null)
        //    {
        //        var props = ReduceBinaryNodeToDictionary(propsNode, "prop");
        //    }
        //}

        Dictionary<string, string> privacySettings;
        private async Task FetchPrivacySettings()
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

        private async Task<Dictionary<string, string>> FetchProps()
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
                        attrs =
                        {
                            { "protocol","2"},
                            { "hash", Creds?.LastPropHash ?? ""}
                        }

                    }
                }

            };
            var resultNode = await Query(fetchProps);
            var propsNode = GetBinaryNodeChild(resultNode, "props");

            Dictionary<string, string> props = new Dictionary<string, string>();

            if (propsNode != null)
            {
                props = ReduceBinaryNodeToDictionary(propsNode, "prop");
                if (Creds != null && props != null)
                {
                    //Creds.LastPropHash = propsNode.attrs["hash"];
                    Creds.LastPropHash = propsNode.attrs.ContainsKey("hash") ? propsNode.attrs["hash"] : Creds.LastPropHash;
                    EV.Emit(EmitType.Update, Creds);
                }
            }

            Logger.Debug("Fetched Props");
            return props;
        }


        public void ChatModify(ChatModification modification, string jid)
        {
            var patch = ChatModificationToAppPatch(modification, jid);
            AppPatch(patch);
        }

        private WAPatchCreate ChatModificationToAppPatch(ChatModification modification, string jid)
        {
            WAPatchCreate patch;
            if (modification is MuteChatModification mute)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        MuteAction = new SyncActionValue.Types.MuteAction()
                        {
                            Muted = mute.Mute > 0,
                            MuteEndTimestamp = mute.Mute.GetValueOrDefault(0)
                        },
                    },
                    Index = ["mute", jid],
                    Type = "regular_high",
                    ApiVersion = 2,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is ArchiveChatModification archive)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        ArchiveChatAction = new SyncActionValue.Types.ArchiveChatAction()
                        {
                            Archived = archive.Archive,
                            MessageRange = GetMessageRange(archive.LastMessages)
                        }
                    },
                    Index = ["archive", jid],
                    Type = "regular_low",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is MarkReadChatModification markRead)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        MarkChatAsReadAction = new SyncActionValue.Types.MarkChatAsReadAction()
                        {
                            Read = markRead.MarkRead,
                            MessageRange = GetMessageRange(markRead.LastMessages)
                        }
                    },
                    Index = ["markChatAsRead", jid],
                    Type = "regular_low",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is ClearChatModification clear)
            {
                var key = clear.LastMessages[0];
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        DeleteMessageForMeAction = new SyncActionValue.Types.DeleteMessageForMeAction()
                        {
                            DeleteMedia = false,
                            MessageTimestamp = key.MessageTimestamp
                        }
                    },
                    Index = ["deleteMessageForMe", jid, key.Key.Id, key.Key.FromMe ? "1" : "0", "0"],
                    Type = "regular_high",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is PinChatModification pin)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        PinAction = new SyncActionValue.Types.PinAction()
                        {
                            Pinned = pin.Pin
                        }
                    },
                    Index = ["pin_v1", jid],
                    Type = "regular_low",
                    ApiVersion = 5,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is StarChatModification star)
            {
                var key = star.Messages[0];
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        StarAction = new SyncActionValue.Types.StarAction()
                        {
                            Starred = star.Star
                        }
                    },
                    Index = ["pin_v1", jid, key.ID, key.FromMe ? "1" : "0", "0"],
                    Type = "regular_low",
                    ApiVersion = 2,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is DeleteChatModification delete)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        DeleteChatAction = new SyncActionValue.Types.DeleteChatAction()
                        {
                            MessageRange = GetMessageRange(delete.LastMessages)
                        }
                    },
                    Index = ["deleteChat", jid, "1"],
                    Type = "regular_high",
                    ApiVersion = 6,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is PushNameChatModification push)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        PushNameSetting = new SyncActionValue.Types.PushNameSetting()
                        {
                            Name = push.PushNameSetting
                        }
                    },
                    Index = ["setting_pushName"],
                    Type = "critical_block",
                    ApiVersion = 1,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is AddChatLableChatModification addchat)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        LabelAssociationAction = new SyncActionValue.Types.LabelAssociationAction()
                        {
                            Labeled = true
                        }
                    },
                    Index = [LabelAssociationType.Chat, addchat.AddChatLabel.LabelID, jid],
                    Type = "regular",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is RemoveChatLableChatModification removechat)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        LabelAssociationAction = new SyncActionValue.Types.LabelAssociationAction()
                        {
                            Labeled = false
                        }
                    },
                    Index = [LabelAssociationType.Chat, removechat.RemoveChatLabel.LabelID, jid],
                    Type = "regular",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is AddMessageLabelChatModification addmessage)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        LabelAssociationAction = new SyncActionValue.Types.LabelAssociationAction()
                        {
                            Labeled = true
                        }
                    },
                    Index = [LabelAssociationType.Message, addmessage.AddMessageLabel.LabelID, jid, addmessage.AddMessageLabel.MessageID, "0", "0"],
                    Type = "regular",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else if (modification is RemoveMessageLabelChatModification removemessage)
            {
                patch = new WAPatchCreate()
                {
                    SyncAction = new SyncActionValue()
                    {
                        LabelAssociationAction = new SyncActionValue.Types.LabelAssociationAction()
                        {
                            Labeled = false
                        }
                    },
                    Index = [LabelAssociationType.Message, removemessage.RemoveMessageLabel.LabelID, jid, removemessage.RemoveMessageLabel.MessageID, "0", "0"],
                    Type = "regular",
                    ApiVersion = 3,
                    Operation = SyncdMutation.Types.SyncdOperation.Set,
                };
            }
            else
            {
                throw new NotSupportedException($"{modification.GetType().FullName} is not supported");
            }

            return patch;
        }

        private SyncActionValue.Types.SyncActionMessageRange GetMessageRange(List<MinimalMessage> lastMessages)
        {
            var messageRange = new SyncActionValue.Types.SyncActionMessageRange();

            var lastMessage = lastMessages.LastOrDefault();
            messageRange.LastMessageTimestamp = lastMessage.MessageTimestamp;
            foreach (var m in lastMessages)
            {
                if (m.Key?.Id == null || m.Key?.RemoteJid == null)
                {
                    throw new Boom("Incomplete key", new BoomData(400));
                }

                if (JidUtils.IsJidGroup(m.Key.RemoteJid) && !m.Key.FromMe && m.Key.Participant != null)
                {
                    throw new Boom("Expected not from me message to have participant", new BoomData(400));
                }
                if (lastMessage?.MessageTimestamp == null || lastMessage.MessageTimestamp == 0)
                {
                    throw new Boom("Missing timestamp in last message list", new BoomData(400));
                }

                if (m.Key.Participant != null)
                {
                    m.Key.Participant = JidUtils.JidNormalizedUser(m.Key.Participant);
                }
                messageRange.Messages.Add(new SyncActionValue.Types.SyncActionMessage()
                {
                    Key = m.Key,
                    Timestamp = m.MessageTimestamp
                });
            }

            return messageRange;
        }

        //TODO: chatModify
        //TODO: star
        //TODO: addChatLabel
        //TODO: removeChatLabel
        //TODO: addMessageLabel
        //TODO: removeMessageLabel

        private async Task ExecuteInitQueries()
        {
            //await FetchAbt();
            await FetchProps();
            //await GetUserTos();
            //await GetUserDisclosures();
            await FetchBlocklist();
            await FetchPrivacySettings();
        }

        private async Task GetUserDisclosures()
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to", S_WHATSAPP_NET },
                    {"type", "get" },
                    {"xmlns","tos" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "get_user_disclosures",
                        attrs =
                        {
                            {"t",DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                        }
                    }
                }
            };
            var result = await Query(node);
            var notice = GetBinaryNodeChildren(result, "notice");
        }

        private async Task GetUserTos()
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to", S_WHATSAPP_NET },
                    {"type", "get" },
                    {"xmlns","tos" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "request",
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "notice",
                                attrs =
                                {
                                    {"id","20230901" },
                                }
                            },
                            new BinaryNode()
                            {
                                tag = "notice",
                                attrs =
                                {
                                    {"id","20230902" },
                                }
                            },
                            new BinaryNode()
                            {
                                tag = "notice",
                                attrs =
                                {
                                    {"id","20240216" },
                                }
                            },
                            new BinaryNode()
                            {
                                tag = "notice",
                                attrs =
                                {
                                    {"id","20231027" },
                                }
                            }
                        }
                    }
                }
            };
            var result = await Query(node);
            var tos = GetBinaryNodeChild(result, "tos");
            var notices = GetBinaryNodeChildren(tos, "notice");

        }





        #endregion
    }
}
