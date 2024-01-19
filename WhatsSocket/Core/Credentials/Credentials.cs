using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Credentials
{

    public class AuthenticationCreds
    {
        [JsonProperty("me")]
        public Contact Me { get; set; }

        [JsonProperty("noiseKey")]
        public KeyPair NoiseKey { get; set; }

        [JsonProperty("pairingEphemeralKeyPair")]
        public KeyPair PairingEphemeralKeyPair { get; set; }

        [JsonProperty("signedIdentityKey")]
        public KeyPair SignedIdentityKey { get; set; }

        [JsonProperty("signedPreKey")]
        public SignedPreKey SignedPreKey { get; set; }

        [JsonProperty("registrationId")]
        public int RegistrationId { get; set; }

        [JsonProperty("advSecretKey")]
        public string AdvSecretKey { get; set; }

        [JsonProperty("processedHistoryMessages")]
        public List<object> ProcessedHistoryMessages { get; set; }

        [JsonProperty("nextPreKeyId")]
        public int NextPreKeyId { get; set; }

        [JsonProperty("firstUnuploadedPreKeyId")]
        public int FirstUnuploadedPreKeyId { get; set; }

        [JsonProperty("accountSyncCounter")]
        public int AccountSyncCounter { get; set; }

        [JsonProperty("accountSettings")]
        public AccountSettings AccountSettings { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("phoneId")]
        public string PhoneId { get; set; }

        [JsonProperty("identityId")]
        public byte[] IdentityId { get; set; }

        [JsonProperty("registered")]
        public bool Registered { get; set; }

        [JsonProperty("backupToken")]
        public byte[] BackupToken { get; set; }

        [JsonProperty("registration")]
        public Registration Registration { get; set; }




        public static string Serialize(AuthenticationCreds? creds)
        {
            return JsonConvert.SerializeObject(creds, Formatting.Indented, new BufferConverter());
        }


        public static AuthenticationCreds? Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<AuthenticationCreds>(json, new BufferConverter());
        }


        private class BufferConverter : JsonConverter<byte[]>
        {
            public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new JsonException();
                }

                //Read first property
                reader.Read();
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    //Make sure it has a property 'type'
                    if (reader.Value?.ToString() == "type")
                    {
                        var value = reader.ReadAsString();
                        if (value == "Buffer")
                        {
                            //Read the data value
                            reader.Read();
                            value = reader.ReadAsString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                existingValue = Convert.FromBase64String(value);
                            }
                        }
                        else
                        {
                            throw new JsonException("the type is not defined as a Buffer");
                        }
                    }

                }
                reader.Read();


                return existingValue;
            }

            public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
            {
                var data = JsonConvert.SerializeObject(new
                {
                    type = "Buffer",
                    data = Convert.ToBase64String(value)
                });

                //writer.WriteStartObject();

                writer.WriteRawValue(data);

                //writer.WriteEndObject();
            }
        }
    }

    public class AccountSettings
    {
        [JsonProperty("unarchiveChats")]
        public bool UnarchiveChats { get; set; }
    }

    //public class Buffer
    //{
    //    [JsonProperty("type")]
    //    public string Type
    //    {
    //        get
    //        {
    //            return "Buffer";
    //        }
    //    }

    //    [JsonProperty("data")]
    //    public string Data { get; set; }


    //    public Buffer()
    //    {

    //    }

    //    public Buffer(byte[] buffer)
    //    {
    //        Data = Convert.ToBase64String(buffer);
    //    }


    //    public static implicit operator byte[](Buffer buffer) => buffer.ToByteArray();
    //    public static explicit operator Buffer(byte[] buffer) => new Buffer(buffer);    


    //    public static implicit operator ByteString(Buffer buffer) => buffer.ToByteString();
    //    public static explicit operator Buffer(ByteString bytes)=> bu

    //    private byte[] ToByteArray()
    //    {
    //        return Convert.FromBase64String(Data);
    //    }

    //    public ByteString ToByteString()
    //    {
    //        return ByteString.FromBase64(Data);
    //    }
    //}

    public class KeyPair
    {
        [JsonProperty("private")]
        public byte[] Private { get; set; }

        [JsonProperty("public")]
        public byte[] Public { get; set; }
    }

    public class SignedPreKey
    {
        [JsonProperty("keyPair")]
        public KeyPair KeyPair { get; set; }

        [JsonProperty("signature")]
        public byte[] Signature { get; set; }

        [JsonProperty("keyId")]
        public int KeyId { get; set; }
    }



    public class Registration
    {
    }



}
