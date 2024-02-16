using Google.Protobuf;
using Org.BouncyCastle.Cms;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.WABinary;
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

                var hasMorePatches = collectionNode.getattr("has_more_patches") == "true";

                if (snapshotNode?.content is byte[] snapShotData)
                {
                    var blobRef = ExternalBlobReference.Parser.ParseFrom(snapShotData);

                    var data = await DownloadExternalBlob(blobRef);
                    var snapshot = SyncdSnapshot.Parser.ParseFrom(data);
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

        private static async Task<byte[]> DownloadExternalBlob(ExternalBlobReference blob)
        {
            var stream = await DownloadContentFromMessage(blob, "md-app-state", new MediaDownloadOptions());
            return stream;
        }


    }
}
