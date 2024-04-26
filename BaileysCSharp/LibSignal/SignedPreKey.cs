using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
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
        [JsonPropertyName("keyPair")]
        public KeyPair KeyPair { get; set; }

        [JsonPropertyName("signature")]
        public byte[] Signature { get; set; }

        [JsonPropertyName("keyId")]
        public ulong KeyId { get; set; }
    }



}
