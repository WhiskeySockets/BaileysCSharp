using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.LibSignal;

namespace BaileysCSharp.Core.Models.SenderKeys
{


    [FolderPrefix("sender-key-memory")]
    public class SenderKeyMemory : Dictionary<string, bool>
    {
        public SenderKeyMemory()
        {

        }
    }

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
            if (keyId == 0 && SenderKeys.Count > 0)
            {
                return SenderKeys[SenderKeys.Count - 1];
            }

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

        internal void SetSenderKeyState(uint id, uint iteration, byte[] chainKey, KeyPair keyPair)
        {
            SenderKeys = [new SenderKeyState(id, iteration, chainKey, keyPair)];
        }
    }


}
