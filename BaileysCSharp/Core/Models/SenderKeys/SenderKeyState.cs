using BaileysCSharp.Core.Helper;
using BaileysCSharp.LibSignal;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Models.SenderKeys
{
    public class SenderKeyState
    {
        const int MAX_MESSAGE_KEYS = 2000;

        [JsonPropertyName("senderKeyId")]
        public uint SenderKeyId { get; set; }

        [JsonPropertyName("senderChainKey")]
        public SenderChainKeyStructure SenderChainKey { get; set; }

        [JsonPropertyName("senderSigningKey")]
        public SenderSigningKeyStructure SenderSigningKey { get; set; }


        [JsonPropertyName("senderMessage")]
        public List<SenderMessageKeyStructure> SenderMessages { get; set; }

        public SenderKeyState()
        {
            SenderMessages = new List<SenderMessageKeyStructure>();
        }

        public SenderKeyState(uint id, uint iteration, byte[] chainKey, KeyPair keyPair)
        {
            SenderKeyId = id;
            SenderChainKey = new SenderChainKeyStructure()
            {
                Iteration = iteration,
                Seed = chainKey
            };
            SenderSigningKey = new SenderSigningKeyStructure()
            {
                Private = keyPair.Private,
                Public = keyPair.Public,
            };
            SenderMessages = new List<SenderMessageKeyStructure>();
        }

        internal void AddSenderMessageKey(SenderMessageKey senderMessageKey)
        {
            var senderMessageKeyStructure = new SenderMessageKeyStructure(senderMessageKey.Iteration, senderMessageKey.Seed);
            SenderMessages.Add(senderMessageKeyStructure);
            if (SenderMessages.Count > MAX_MESSAGE_KEYS)
            {
                SenderMessages.RemoveAt(0);
            }
        }

        internal SenderChainKey GetSenderChainKey()
        {
            return new SenderChainKey(SenderChainKey.Iteration, SenderChainKey.Seed);
        }

        internal bool HasSenderMessageKey(uint iteration)
        {
            foreach (var senderMessageKey in SenderMessages)
            {
                if (senderMessageKey.Iteration == iteration)
                {
                    return true;
                }
            }
            return false;
        }

        internal SenderMessageKey? RemoveSenderMessageKey(uint iteration)
        {
            SenderMessageKeyStructure? result = default;
            foreach (var senderMessageKey in SenderMessages)
            {
                if (senderMessageKey.Iteration == iteration)
                {
                    result = senderMessageKey;
                }
            }
            if (result != null)
                return new SenderMessageKey(iteration, result.Seed);
            return null;
        }

        internal string? Serialize()
        {
            return JsonSerializer.Serialize(this, JsonHelper.Options);
        }

        internal void SetSenderChainKey(SenderChainKey chainKey)
        {
            SenderChainKey = new SenderChainKeyStructure()
            {
                Iteration = chainKey.Iteration,
                Seed = chainKey.ChainKey
            };
        }

        public byte[] GetSigningKeyPrivate()
        {
            return SenderSigningKey.Private;
        }
        public byte[] GetSigningKeyPublic()
        {
            return SenderSigningKey.Public;
        }
    }


}
