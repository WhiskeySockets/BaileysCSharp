using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Compiler;
using Org.BouncyCastle.Cms;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Core.WABinary;
using BaileysCSharp.Exceptions;
using static BaileysCSharp.Core.Utils.MediaMessageUtil;
using static BaileysCSharp.Core.Utils.GenericUtils;
using BaileysCSharp.Core.Events;
using System.Runtime.CompilerServices;
using BaileysCSharp.Core.Types;
using System.Text.Json;

namespace BaileysCSharp.Core.Utils
{

    public class ChatUtils
    {
        public static async Task<Dictionary<string, SyncPatchesResult>> ExtractSyncedPathces(BinaryNode result)
        {
            var final = new Dictionary<string, SyncPatchesResult>();
            var syncNode = GetBinaryNodeChild(result, "sync");
            var collectionNodes = GetBinaryNodeChildren(syncNode, "collection");

            foreach (var collectionNode in collectionNodes)
            {
                var patchesNode = GetBinaryNodeChild(collectionNode, "patches");
                var patches = GetBinaryNodeChildren(patchesNode ?? collectionNode, "patch");
                var snapshotNode = GetBinaryNodeChild(collectionNode, "snapshot");
                var syncds = new List<SyncdPatch>();
                var name = collectionNode.getattr("name");
                if (!string.IsNullOrWhiteSpace(name))
                {

                    var hasMorePatches = collectionNode.getattr("has_more_patches") == "true";

                    SyncdSnapshot? snapshot = default(SyncdSnapshot);
                    if (snapshotNode?.content is byte[] snapShotData)
                    {
                        var blobRef = ExternalBlobReference.Parser.ParseFrom(snapShotData);

                        var data = await DownloadExternalBlob(blobRef);
                        snapshot = SyncdSnapshot.Parser.ParseFrom(data);
                    }

                    foreach (var item in patches)
                    {
                        if (item.content is byte[] content)
                        {
                            var syncd = SyncdPatch.Parser.ParseFrom(content);
                            if (syncd.Version == null || syncd.Version.Version == 0)
                            {
                                syncd.Version = new SyncdVersion()
                                {
                                    Version = Convert.ToUInt64(collectionNode.getattr("version")) + 1
                                };
                            }
                            syncds.Add(syncd);
                        }
                    }
                    final[name] = new SyncPatchesResult()
                    {
                        Patches = syncds,
                        HasMorePatches = hasMorePatches,
                        Snapshot = snapshot
                    };
                }

            }



            return final;
        }



        internal static async Task<(AppStateSyncVersion state, ChatMutationMap mutationMap)> DecodePatches(string name, List<SyncdPatch> syncds, AppStateSyncVersion appStateSyncVersion, BaseKeyStore keys, ulong minimumVersionNumber, Logger logger, bool validateMacs)
        {
            var newState = new AppStateSyncVersion();
            newState.Version = appStateSyncVersion.Version;
            newState.IndexValueMap = appStateSyncVersion.IndexValueMap;

            var mutationMap = new ChatMutationMap();
            for (int i = 0; i < syncds.Count; i++)
            {
                var syncd = syncds[i];
                if (syncd.ExternalMutations != null)
                {

                    logger?.Trace(new { name, syncd.Version }, "downloading external patch");

                    var @ref = await DownloadExternalPatch(syncd.ExternalMutations);

                    logger?.Debug(new { name, syncd.Version, mutations = 1 }, "downloaded external patch");
                    syncd.Mutations.Add(@ref.Mutations);
                }

                var patchVersion = syncd.Version.Version;
                newState.Version = patchVersion;

                var shouldMutate = patchVersion > minimumVersionNumber;

                var onChatMutation = (ChatMutation mutation) =>
                {
                    if (mutation != null)
                    {
                        var indexStr = Encoding.UTF8.GetString(mutation.SyncAction.Index.ToByteArray());
                        var index = JsonSerializer.Deserialize<string[]>(indexStr);
                        if (index != null)
                        {
                            mutationMap[index[0]] = mutation;
                        }
                    }
                };

                var decodeResult = DecodeSyncdPatch(syncd, name, newState, keys, onChatMutation, true);

                newState.Hash = decodeResult.Hash;
                newState.IndexValueMap = decodeResult.IndexValueMap;
                if (validateMacs)
                {
                    var base64Key = syncd.KeyId.ToByteArray().ToBase64();
                    var keyEnc = keys.Get<AppStateSyncKeyStructure>(base64Key);
                    //var keyEnc = appStateSyncKeyStore.Get(base64Key);
                    if (keyEnc == null)
                    {
                        throw new Boom("failed to find key {base64Key} to decode mutation");
                    }

                    var result = MutationKeys(keyEnc.KeyData);
                    var computedSnapshotMac = GenerateSnapshotMac(newState.Hash, newState.Version, name, result.SnapshotMacKey);
                    if (syncd.SnapshotMac.ToByteArray().Compare(computedSnapshotMac) != 0)
                        throw new Boom($"failed to verify LTHash at ${newState.Version} of ${name}");
                }

                // clear memory used up by the mutations
                syncd.Mutations.Clear();
            }


            return (newState, mutationMap);
        }

        private static byte[] GenerateSnapshotMac(byte[] lthash, ulong version, string name, byte[] key)
        {
            var versionMac = to64BitNetworkOrder(version);
            var total = lthash.Concat(versionMac).Concat(Encoding.UTF8.GetBytes(name)).ToArray();
            return Helper.CryptoUtils.HmacSign(total, key);
        }

        private static AppStateSyncVersion DecodeSyncdPatch(SyncdPatch msg, string name, AppStateSyncVersion initialState, BaseKeyStore keys, Action<ChatMutation> onChatMutation, bool validateMacs)
        {
            if (validateMacs)
            {
                var base64Key = msg.KeyId.Id.ToBase64();
                var mainKeyObj = keys.Get<AppStateSyncKeyStructure>(base64Key);
                //var mainKeyObj = appStateSyncKeyStore.Get(base64Key);
                if (mainKeyObj == null)
                {
                    throw new Boom($"failed to find key '{base64Key}' to decode patch", Events.DisconnectReason.NoKeyForMutation);
                }

                var mainKey = MutationKeys(mainKeyObj.KeyData);
                var mutationmac = msg.Mutations.Select(x => x.Record.Value.Blob.ToByteArray().Slice(-32));

                var patchMac = GeneratePatchMac(msg.SnapshotMac.ToByteArray(), mutationmac, msg.Version.Version, name, mainKey.PatchMacKey);
                if (patchMac.Compare(msg.PatchMac.ToByteArray()) != 0)
                    throw new Boom("Invalid Patch Mac");
            }

            var result = DecodeSyncdMutations(msg.Mutations, initialState, keys, onChatMutation, validateMacs);
            return result;
        }

        private static byte[] GeneratePatchMac(byte[] snapshotMac, IEnumerable<byte[]> valueMacs, ulong version, string type, byte[] key)
        {
            var versionMac = to64BitNetworkOrder(version);
            var total = snapshotMac.Concat(valueMacs.Map(x => x)).Concat(versionMac).Concat(Encoding.UTF8.GetBytes(type)).ToArray();
            return Helper.CryptoUtils.HmacSign(total, key);
        }

        private static byte[] to64BitNetworkOrder(ulong version)
        {
            BlobWriter writer = new BlobWriter(8);
            writer.WriteUInt64(version);
            return writer.ToArray().Reverse().ToArray();
        }

        private static async Task<SyncdMutations> DownloadExternalPatch(ExternalBlobReference externalMutations)
        {
            var buffer = await DownloadExternalBlob(externalMutations);
            return SyncdMutations.Parser.ParseFrom(buffer);
        }

        internal static (AppStateSyncVersion state, ChatMutationMap mutationMap) DecodeSyncdSnapshot(string name, SyncdSnapshot snapshot, BaseKeyStore keys, ulong minimumVersionNumber, Logger logger, bool validateMacs)
        {
            var newState = new AppStateSyncVersion();
            newState.Version = snapshot.Version.Version;

            var mutationMap = new ChatMutationMap();
            bool areMutationsRequired = minimumVersionNumber == 0 || newState.Version > minimumVersionNumber;

            var onChatMutation = (ChatMutation mutation) =>
            {
                if (areMutationsRequired)
                {
                    var indexStr = Encoding.UTF8.GetString(mutation.SyncAction.Index.ToByteArray());
                    var index = JsonSerializer.Deserialize<string[]>(indexStr);
                    if (index != null)
                    {
                        mutationMap[index[0]] = mutation;
                    }
                }
            };


            var decoded = DecodeSyncdMutations(snapshot.Records, newState, keys, onChatMutation, validateMacs);

            newState.Hash = decoded.Hash;
            newState.IndexValueMap = decoded.IndexValueMap;

            if (validateMacs)
            {
                var base64Key = snapshot.KeyId.Id.ToBase64();
                var keyEnc = keys.Get<AppStateSyncKeyStructure>(base64Key);
                if (keyEnc == null)
                {
                    throw new Boom($"failed to find key '{base64Key}' to decode patch", Events.DisconnectReason.NoKeyForMutation);
                }

                var result = MutationKeys(keyEnc.KeyData);
                var computedSnapshotMac = GenerateSnapshotMac(newState.Hash, newState.Version, name, result.SnapshotMacKey);
                if (snapshot.Mac.ToByteArray().Compare(computedSnapshotMac) != 0)
                    throw new Boom($"failed to verify LTHash at {newState.Version} of ${name} from snapshot");
            }


            return (newState, mutationMap);
        }

        private static AppStateSyncVersion DecodeSyncdMutations(RepeatedField<SyncdRecord> records, AppStateSyncVersion initialState, BaseKeyStore keys, Action<ChatMutation> onMutation, bool validateMacs)
        {
            var ltGenerator = new HashGenerator(initialState);
            foreach (var record in records)
            {
                DecodeSyncdMutation(ltGenerator, record, SyncdMutation.Types.SyncdOperation.Set, keys, validateMacs, onMutation);
            }
            return ltGenerator.Finish();
        }
        private static AppStateSyncVersion DecodeSyncdMutations(RepeatedField<SyncdMutation> msgMutations, AppStateSyncVersion initialState, BaseKeyStore keys, Action<ChatMutation> onMutation, bool validateMacs)
        {
            var ltGenerator = new HashGenerator(initialState);
            foreach (var item in msgMutations)
            {
                DecodeSyncdMutation(ltGenerator, item.Record, item.Operation, keys, validateMacs, onMutation);
            }
            return ltGenerator.Finish();
        }

        private static void DecodeSyncdMutation(HashGenerator ltGenerator, SyncdRecord record, SyncdMutation.Types.SyncdOperation operation, BaseKeyStore keys, bool validateMacs, Action<ChatMutation> onMutation)
        {
            var key = GetKey(record.KeyId.Id, keys);
            var content = record.Value.Blob.ToByteArray();
            var encContent = content.Slice(0, -32);
            var ogValueMac = content.Slice(-32);
            if (validateMacs)
            {
                var contenthMac = GenerateMac(operation, encContent, record.KeyId.Id.ToByteArray(), key.ValueMacKey);
                if (contenthMac.Compare(ogValueMac) != 0)
                    throw new Boom("HMAC content verification failed");
            }

            var result = Helper.CryptoUtils.DecryptAesCbc(encContent, key.ValueEncryptionKey);
            var syncAction = SyncActionData.Parser.ParseFrom(result);


            if (validateMacs)
            {
                var hmac = Helper.CryptoUtils.HmacSign(syncAction.Index.ToByteArray(), key.IndexKey);
                if (hmac.Compare(record.Index.Blob.ToByteArray()) != 0)
                    throw new Boom("HMAC index verification failed");
            }

            var indexStr = Encoding.UTF8.GetString(syncAction.Index.ToByteArray());
            onMutation(new ChatMutation() { SyncAction = syncAction, Index = JsonSerializer.Deserialize<string[]>(indexStr) });

            ltGenerator.Mix(record.Index.Blob, ogValueMac, operation);
        }

        private static byte[] GenerateMac(SyncdMutation.Types.SyncdOperation operation, byte[] data, byte[] keyId, byte[] key)
        {
            byte[] r = [0x01];
            switch (operation)
            {
                case SyncdMutation.Types.SyncdOperation.Set:
                    r = [0x01];
                    break;
                case SyncdMutation.Types.SyncdOperation.Remove:
                    r = [0x02];
                    break;
                default:
                    break;
            }
            var keyData = r.Concat(keyId).ToArray();

            var last = new byte[8];
            last.Set([(byte)keyData.Length], last.Length - 1);

            var total = keyData.Concat(data).Concat(last).ToArray();
            var hmac = Helper.CryptoUtils.HmacSign(total, key, "sha512");

            return hmac.Slice(0, 32);
        }

        private static MutationKey GetKey(ByteString id, BaseKeyStore keys)
        {
            var base64Key = id.ToBase64();
            var keyEnc = keys.Get<AppStateSyncKeyStructure>(base64Key);
            if (keyEnc == null)
                throw new Boom("Failed to find any key", Events.DisconnectReason.NoKeyForMutation);

            return MutationKeys(keyEnc.KeyData);
        }

        private static MutationKey MutationKeys(byte[] keyData)
        {
            var expanded = Helper.CryptoUtils.HKDF(keyData, 160, [], Encoding.UTF8.GetBytes("WhatsApp Mutation Keys"));
            return new MutationKey()
            {
                IndexKey = expanded.Slice(0, 32),
                ValueEncryptionKey = expanded.Slice(32, 64),
                ValueMacKey = expanded.Slice(64, 96),
                SnapshotMacKey = expanded.Slice(96, 128),
                PatchMacKey = expanded.Slice(128, 160)
            };
        }


        private static async Task<byte[]> DownloadExternalBlob(ExternalBlobReference blob)
        {
            var stream = await DownloadContentFromMessage(blob, "md-app-state", new MediaDownloadOptions());
            return stream;
        }

        internal static void ProcessSyncAction(ChatMutation syncAction, EventEmitter eV, AuthenticationCreds creds, AccountSettings? accountSettings, MemoryStore store, Logger logger)
        {
            var isInitialSync = accountSettings == null;

            logger?.Trace(new { syncAction, initialSync = isInitialSync }, "processing sync action");

            var action = syncAction.SyncAction.Value;

            if (syncAction.Index.Length < 4)
            {
                var index = syncAction.Index;
                Array.Resize(ref index, 4);
                syncAction.Index = index;
            }

            var type = syncAction.Index[0];
            var id = syncAction.Index[1];
            var msgId = syncAction.Index[2];
            var fromMe = syncAction.Index[3];

            if (action?.MuteAction != null)
            {
                var chat = store.GetChat(id);
                if (chat != null)
                {
                    chat.MuteEndTime = action.MuteAction.Muted == true ? action.MuteAction.MuteEndTimestamp : 0;
                    eV.Emit(EmitType.Update, chat);
                }
            }
            else if (action?.ArchiveChatAction != null || type == "archive" || type == "unarchive")
            {
                // okay so we've to do some annoying computation here
                // when we're initially syncing the app state
                // there are a few cases we need to handle
                // 1. if the account unarchiveChats setting is true
                //   a. if the chat is archived, and no further messages have been received -- simple, keep archived
                //   b. if the chat was archived, and the user received messages from the other person afterwards
                //		then the chat should be marked unarchved --
                //		we compare the timestamp of latest message from the other person to determine this
                // 2. if the account unarchiveChats setting is false -- then it doesn't matter,
                //	it'll always take an app state action to mark in unarchived -- which we'll get anyway
                var archiveAction = action?.ArchiveChatAction;
                var isArchived = archiveAction != null
                    ? archiveAction.Archived
                    : type == "archive";

                var chat = store.GetChat(id);
                if (chat != null)
                {
                    chat.Archived = isArchived;
                    eV.Emit(EmitType.Update, chat);
                }
            }
            else if (action?.MarkChatAsReadAction != null)
            {
                var markReadAction = action.MarkChatAsReadAction;
                // basically we don't need to fire an "read" update if the chat is being marked as read
                // because the chat is read by default
                // this only applies for the initial sync
                var isNullUpdate = isInitialSync && markReadAction.Read;

                var chat = store.GetChat(id);
                if (chat != null)
                {
                    chat.UnreadCount = (ulong)(isNullUpdate ? 0L : (markReadAction.Read ? 0L : -1L));
                    eV.Emit(EmitType.Update, chat);
                }

            }
            else if (action?.DeleteMessageForMeAction != null || type == "deleteMessageForMe")
            {
                var message = store.GetMessage(id);
                if (message != null)
                {
                    var msg = message.ToMessageInfo();
                    eV.Emit(EmitType.Delete, new MessageEventModel(MessageEventType.Delete, msg));
                }
            }
            else if (action?.ContactAction != null)
            {
                var contact = new ContactModel() { ID = id, Name = action.ContactAction.FullName };
                eV.Emit(EmitType.Upsert, contact);
            }
            else if (action?.PushNameSetting != null)
            {
                var name = action.PushNameSetting.Name;
                if (creds.Me.Name != name)
                {
                    creds.Me.Name = name;
                    eV.Emit(EmitType.Update, creds);
                }
            }
            else if (action?.PinAction != null)
            {
                var chat = store.GetChat(id);
                if (chat != null)
                {
                    chat.Pinned = action.PinAction.Pinned ? action.Timestamp : 0;
                    eV.Emit(EmitType.Update, chat);
                }
            }
            else if (action?.UnarchiveChatsSetting != null)
            {
                var unarchiveChats = action.UnarchiveChatsSetting.UnarchiveChats;
                creds.AccountSettings.UnarchiveChats = unarchiveChats;
                eV.Emit(EmitType.Update, creds);
            }
            else if (action?.StarAction != null)
            {
                var starred = action.StarAction.Starred;
                var message = store.GetMessage(id);
                if (message != null)
                {
                    var msg = message.ToMessageInfo();
                    msg.Starred = starred;
                    eV.Emit(EmitType.Update, new MessageEventModel(MessageEventType.Update, msg));
                }

            }
            else if (action?.DeleteChatAction != null)
            {
                var chat = store.GetContact(id);
                if (chat != null)
                {
                    eV.Emit(EmitType.Delete, chat);
                }
            }
            else if (action?.LabelEditAction != null)
            {

            }
            else if (action?.LabelAssociationAction != null)
            {

            }
        }
    }
}
