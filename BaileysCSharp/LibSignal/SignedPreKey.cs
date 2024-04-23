using Newtonsoft.Json;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using BaileysCSharp.LibSignal;

namespace BaileysCSharp.Core.Models
{
    public class SignedPreKey : PreKeyPair
    {
        public byte[] Signature { get; set; }
    }
    
    //For Compatibility Conversion
    public class SignedPreKeyModel
    {
        [JsonProperty("keyPair")]
        public KeyPair KeyPair { get; set; }

        [JsonProperty("signature")]
        public byte[] Signature { get; set; }

        [JsonProperty("keyId")]
        public ulong KeyId { get; set; }
    }



}
