using Proto;
using System.Diagnostics.CodeAnalysis;
using BaileysCSharp.LibSignal;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Converters;
using System.Text.Json.Serialization;
using System.Text.Json;
using BaileysCSharp.Core.Helper;

namespace BaileysCSharp.Core.Types
{
    //public class ProcessedHistoryMessageKey
    //{
    //    [JsonProperty("remoteJid")]
    //    public string RemoteJid { get; set; }

    //    [JsonProperty("fromMe")]
    //    public bool FromMe { get; set; }

    //    [JsonProperty("id")]
    //    public string Id { get; set; }
    //}

    public class ProcessedHistoryMessage
    {
        [JsonPropertyName("key")]
        public MessageKey Key { get; set; }

        [JsonPropertyName("messageTimestamp")]
        public ulong MessageTimestamp { get; set; }
    }

    public class Account
    {
        [JsonPropertyName("details")]
        public byte[] Details { get; set; }

        [JsonPropertyName("accountSignatureKey")]
        public byte[] AccountSignatureKey { get; set; }

        [JsonPropertyName("accountSignature")]
        public byte[] AccountSignature { get; set; }

        [JsonPropertyName("deviceSignature")]
        public byte[] DeviceSignature { get; set; }
    }

    public partial class AuthenticationCreds
    {
        public AuthenticationCreds()
        {
            ProcessedHistoryMessages = new List<ProcessedHistoryMessage>();
        }

        [JsonPropertyName("me")]
        public ContactModel Me { get; set; }

        [JsonPropertyName("noiseKey")]
        public KeyPair NoiseKey { get; set; }

        [JsonPropertyName("pairingEphemeralKeyPair")]
        public KeyPair PairingEphemeralKeyPair { get; set; }

        [JsonPropertyName("signedIdentityKey")]
        public KeyPair SignedIdentityKey { get; set; }

        [JsonPropertyName("signedPreKey")]
        public SignedPreKey SignedPreKey { get; set; }

        [JsonPropertyName("registrationId")]
        public int RegistrationId { get; set; }

        [JsonPropertyName("advSecretKey")]
        public string AdvSecretKey { get; set; }

        [JsonPropertyName("processedHistoryMessages")]
        public List<ProcessedHistoryMessage> ProcessedHistoryMessages { get; set; }

        [JsonPropertyName("nextPreKeyId")]
        public uint NextPreKeyId { get; set; }

        [JsonPropertyName("firstUnuploadedPreKeyId")]
        public uint FirstUnuploadedPreKeyId { get; set; }

        [JsonPropertyName("accountSyncCounter")]
        public int AccountSyncCounter { get; set; }

        [JsonPropertyName("accountSettings")]
        public AccountSettings AccountSettings { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("phoneId")]
        public string PhoneId { get; set; }

        [JsonPropertyName("identityId")]
        public byte[] IdentityId { get; set; }

        [JsonPropertyName("registered")]
        public bool Registered { get; set; }

        [JsonPropertyName("backupToken")]
        public byte[] BackupToken { get; set; }

        [JsonPropertyName("registration")]
        public Registration Registration { get; set; }

        [JsonPropertyName("account")]
        public Account Account { get; set; }

        [JsonPropertyName("signalIdentities")]
        public SignalIdentity[] SignalIdentities { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }


        [JsonPropertyName("myAppStateKeyId")]
        public string MyAppStateKeyId { get; set; }

        public static string Serialize(AuthenticationCreds? creds)
        {
            return JsonSerializer.Serialize(creds, JsonHelper.BufferOptions);
        }


        public static AuthenticationCreds? Deserialize(string json)
        {
            var data = JsonSerializer.Deserialize<AuthenticationCreds>(json, JsonHelper.BufferOptions);
            return data;
        }

        public ulong? LastAccountTypeSync { get; set; }
        public byte[] RoutingInfo { get; set; }
        public string? LastPropHash { get; internal set; }
    }


}
