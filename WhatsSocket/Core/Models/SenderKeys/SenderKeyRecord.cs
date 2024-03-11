using Newtonsoft.Json;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models.SenderKeys
{
    [FolderPrefix("sender-key")]
    public class SenderKeyRecord
    {
        public List<SenderKeyState> SenderKeys { get; set; }
        public SenderKeyRecord(string fileName)
        {
            if (File.Exists(fileName))
            {
                SenderKeys = JsonConvert.DeserializeObject<List<SenderKeyState>>(File.ReadAllText(fileName));
            }
            else
            {
                SenderKeys = new List<SenderKeyState>();
            }
        }
        public SenderKeyRecord()
        {
            SenderKeys = new List<SenderKeyState>();
        }

        public bool IsEmpty
        {
            get
            {
                return SenderKeys.Count == 0;
            }
        }

        public SenderKeyState? GetSenderStateKey(uint keyId)
        {
            return SenderKeys.FirstOrDefault(x => x.SenderKeyId == keyId);
        }


        internal string? Serialize()
        {
            return JsonConvert.SerializeObject(SenderKeys, Formatting.Indented);
        }

        internal void AddSenderKeyState(uint id, uint iteration, byte[] chainKey, byte[] signatureKey)
        {
            SenderKeys.Add(new SenderKeyState()
            {
                SenderKeyId = id,
                SenderChainKey = new SenderChainKeyStructure()
                {
                    Iteration = iteration,
                    Seed = chainKey
                },
                SenderSigningKey = new SenderSigningKeyStructure()
                {
                    Public = signatureKey
                }
            });
            if (SenderKeys.Count > 5)
            {
                SenderKeys.RemoveAt(0);
            }
        }
    }


}
