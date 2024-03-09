using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Compiler;
using Newtonsoft.Json;
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
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.WABinary;
using WhatsSocket.Exceptions;
using static Google.Protobuf.WellKnownTypes.Field.Types;
using static Proto.ContextInfo.Types.AdReplyInfo.Types;
using static WhatsSocket.Core.Utils.GenericUtils;
using static WhatsSocket.Core.Utils.MediaMessageUtil;

namespace WhatsSocket.Core.Utils
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



        internal static async Task<(AppStateSyncVersion state, Dictionary<string, ChatMutation> mutationMap)> DecodePatches(string name, List<SyncdPatch> syncds, AppStateSyncVersion appStateSyncVersion, AppStateSyncKeyStore appStateSyncKeyStore, ulong minimumVersionNumber, Logger logger, bool validateMacs)
        {
            var newState = new AppStateSyncVersion();
            newState.Version = appStateSyncVersion.Version;
            newState.IndexValueMap = appStateSyncVersion.IndexValueMap;

            var mutationMap = new Dictionary<string, ChatMutation>();
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
                        var index = mutation.SyncAction.Index.ToString();
                        if (index != null)
                        {
                            mutationMap[index] = mutation;
                        }
                    }
                };

                var decodeResult = DecodeSyncdPatch(syncd, name, newState, appStateSyncKeyStore, onChatMutation, true);

                newState.Hash = decodeResult.Hash;
                newState.IndexValueMap = decodeResult.IndexValueMap;
                if (validateMacs)
                {
                    var base64Key = syncd.KeyId.ToByteArray().ToBase64();
                    var keyEnc = appStateSyncKeyStore.Get(base64Key);
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
            return EncryptionHelper.HmacSign(total, key);
        }

        private static AppStateSyncVersion DecodeSyncdPatch(SyncdPatch msg, string name, AppStateSyncVersion initialState, AppStateSyncKeyStore appStateSyncKeyStore, Action<ChatMutation> onChatMutation, bool validateMacs)
        {
            if (validateMacs)
            {
                var base64Key = msg.KeyId.Id.ToBase64();
                var mainKeyObj = appStateSyncKeyStore.Get(base64Key);
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

            var result = DecodeSyncdMutations(msg.Mutations, initialState, appStateSyncKeyStore, onChatMutation, validateMacs);
            return result;
        }

        private static byte[] GeneratePatchMac(byte[] snapshotMac, IEnumerable<byte[]> valueMacs, ulong version, string type, byte[] key)
        {
            var versionMac = to64BitNetworkOrder(version);
            var total = snapshotMac.Concat(valueMacs.Map(x => x)).Concat(versionMac).Concat(Encoding.UTF8.GetBytes(type)).ToArray();
            return EncryptionHelper.HmacSign(total, key);
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

        internal static (AppStateSyncVersion state, Dictionary<string, ChatMutation> mutationMap) DecodeSyncdSnapshot(string name, SyncdSnapshot snapshot, AppStateSyncKeyStore appStateSyncKeyStore, ulong minimumVersionNumber, Logger logger, bool validateMacs)
        {
            var newState = new AppStateSyncVersion();
            newState.Version = snapshot.Version.Version;

            var mutationMap = new Dictionary<string, ChatMutation>();
            bool areMutationsRequired = minimumVersionNumber == 0 || newState.Version > minimumVersionNumber;

            var onChatMutation = (ChatMutation mutation) =>
            {
                if (areMutationsRequired)
                {
                    var index = mutation.SyncAction.Index.ToString();
                    if (index != null)
                    {
                        mutationMap[index] = mutation;
                    }
                }
            };


            var decoded = DecodeSyncdMutations(snapshot.Records, newState, appStateSyncKeyStore, onChatMutation, validateMacs);

            newState.Hash = decoded.Hash;
            newState.IndexValueMap = decoded.IndexValueMap;

            if (validateMacs)
            {
                var base64Key = snapshot.KeyId.Id.ToBase64();
                var keyEnc = appStateSyncKeyStore.Get(base64Key);
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

        private static AppStateSyncVersion DecodeSyncdMutations(RepeatedField<SyncdRecord> records, AppStateSyncVersion initialState, AppStateSyncKeyStore getAppStateSyncKey, Action<ChatMutation> onMutation, bool validateMacs)
        {
            var ltGenerator = new HashGenerator(initialState);
            foreach (var record in records)
            {
                DecodeSyncdMutation(ltGenerator, record, SyncdMutation.Types.SyncdOperation.Set, getAppStateSyncKey, validateMacs, onMutation);
            }
            return ltGenerator.Finish();
        }
        private static AppStateSyncVersion DecodeSyncdMutations(RepeatedField<SyncdMutation> msgMutations, AppStateSyncVersion initialState, AppStateSyncKeyStore getAppStateSyncKey, Action<ChatMutation> onMutation, bool validateMacs)
        {
            var ltGenerator = new HashGenerator(initialState);
            foreach (var item in msgMutations)
            {
                DecodeSyncdMutation(ltGenerator, item.Record, item.Operation, getAppStateSyncKey, validateMacs, onMutation);
            }
            return ltGenerator.Finish();
        }

        private static void DecodeSyncdMutation(HashGenerator ltGenerator, SyncdRecord record, SyncdMutation.Types.SyncdOperation operation, AppStateSyncKeyStore getAppStateSyncKey, bool validateMacs, Action<ChatMutation> onMutation)
        {
            var key = GetKey(record.KeyId.Id, getAppStateSyncKey);
            var content = record.Value.Blob.ToByteArray();
            var encContent = content.Slice(0, -32);
            var ogValueMac = content.Slice(-32);
            if (validateMacs)
            {
                var contenthMac = GenerateMac(operation, encContent, record.KeyId.Id.ToByteArray(), key.ValueMacKey);
                if (contenthMac.Compare(ogValueMac) != 0)
                    throw new Boom("HMAC content verification failed");
            }

            var result = EncryptionHelper.DecryptAesCbc(encContent, key.ValueEncryptionKey);
            var syncAction = SyncActionData.Parser.ParseFrom(result);


            if (validateMacs)
            {
                var hmac = EncryptionHelper.HmacSign(syncAction.Index.ToByteArray(), key.IndexKey);
                if (hmac.Compare(record.Index.Blob.ToByteArray()) != 0)
                    throw new Boom("HMAC index verification failed");
            }

            var indexStr = Encoding.UTF8.GetString(syncAction.Index.ToByteArray());
            onMutation(new ChatMutation() { SyncAction = syncAction, Index = JsonConvert.DeserializeObject<string[]>(indexStr) });

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
            var hmac = EncryptionHelper.HmacSign(total, key, "sha512");

            return hmac.Slice(0, 32);
        }

        private static MutationKey GetKey(ByteString id, AppStateSyncKeyStore getAppStateSyncKey)
        {
            var base64Key = id.ToBase64();
            var keyEnc = getAppStateSyncKey.Get(base64Key);
            if (keyEnc == null)
                throw new Boom("Failed to find any key", Events.DisconnectReason.NoKeyForMutation);

            return MutationKeys(keyEnc.KeyData);
        }

        private static MutationKey MutationKeys(byte[] keyData)
        {
            var expanded = EncryptionHelper.HKDF(keyData, 160, [], Encoding.UTF8.GetBytes("WhatsApp Mutation Keys"));
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


    }
}
