using Proto;

namespace WhatsSocket.Core.Utils
{
    public class SyncPatchesResult
    {
        public bool HasMorePatches { get; set; }
        public List< SyncdPatch> Patches { get; set; }
        public SyncdSnapshot Snapshot { get; set; }

        public SyncPatchesResult()
        {
            Patches = new List<SyncdPatch>();
        }
    }
}
