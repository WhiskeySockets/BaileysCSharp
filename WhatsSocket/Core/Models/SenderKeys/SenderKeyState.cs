using Newtonsoft.Json;

namespace WhatsSocket.Core.Models.SenderKeys
{
    public class SenderKeyState
    {
        const int MAX_MESSAGE_KEYS = 2000;

        [JsonProperty("senderKeyId")]
        public uint SenderKeyId { get; set; }

        [JsonProperty("senderChainKey")]
        public SenderChainKeyStructure SenderChainKey { get; set; }

        [JsonProperty("senderSigningKey")]
        public SenderSigningKeyStructure SenderSigningKey { get; set; }


        [JsonProperty("senderMessage")]
        public List<SenderMessageKeyStructure> SenderMessages { get; set; }

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
            return JsonConvert.SerializeObject(this);
        }

        internal void SetSenderChainKey(SenderChainKey chainKey)
        {
            SenderChainKey = new SenderChainKeyStructure()
            {
                Iteration = chainKey.Iteration,
                Seed = chainKey.ChainKey
            };
        }
    }


}
