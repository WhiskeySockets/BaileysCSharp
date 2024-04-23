using Newtonsoft.Json;
using Proto;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Textsecure;
using BaileysCSharp.Core.Extensions;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Signal;
using BaileysCSharp.Core.Utils;
using BaileysCSharp.Core.WABinary;
using BaileysCSharp.LibSignal;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }


        [Test]
        public static void TestDecodeWhisperMessage()
        {
            var config = new SocketConfig()
            {
                SessionName = "CreateSession",
            };
            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();
            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);
            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };
            var storage = new SignalStorage(config.Auth);
            var sessionFile = Path.Join(config.CacheRoot, "session", $"27665245067.0.json");
            //
            if (File.Exists(sessionFile))
            {
                File.Delete(sessionFile);
            }


            SessionBuilder.SendKeyPair = new KeyPair()
            {
                Public = Convert.FromBase64String("BWsNAT3phMcvy/RWV6a7YLpQ3ItR5K4Gxk+IBoAN7oc7"),
                Private = Convert.FromBase64String("0DZTxh4maIVzBjqAxLVD2Mu4Oztn/txWtDbcXVVRmlA=")
            };

            SessionCipher.EphemeralKeyPair = new KeyPair()
            {
                Private = Convert.FromBase64String("YMo0xElvzL4B8sUM4wysBbiVy3CcDRPvRtB2470+Amc="),
                Public = Convert.FromBase64String("Bai5PQJsxdIDXWRLAIm8FFe4EPp8jOInm/ElXUhKSRYf")
            };

            var data = Convert.FromBase64String("MwgHEiEF51qjnXcsRw0BrXzGAYZ8VCpxFV+1Szg483wTzJVoWlAaIQWBMy0fikSwDpotqPycui0iPIt0yeJAScZlVKprz+uORSJSMwohBW5Ysg2vPDJVNeNjB3jbtnDAjZCnHatgNnPGsa2XOoIWEAAYACIg63vX5iFT/NLxuC/qdyDWp/KjS8zbemqvTogJoWbzn/EFuDqf2goX2Sjz3pTzBDAB");
            var cipher = new SessionCipher(storage, new ProtocolAddress("27665245067@whatsapp.net"));
            cipher.DecryptPreKeyWhisperMessage(data);

            var preKeyProto = PreKeyWhisperMessage.Parser.ParseFrom(data.Slice(1));
            var record = cipher.GetRecord();
            var session = record.GetSession(preKeyProto.BaseKey.ToBase64());

            if (session.Chains["BW5Ysg2vPDJVNeNjB3jbtnDAjZCnHatgNnPGsa2XOoIW"].ChainKey.Key.ToBase64() == "Ta+oOkXWh4uB53EkiuOy2VTgG9M5k6sDVVnKhOAzRtA=")
            {

            }

            SessionBuilder.SendKeyPair = null;
            SessionCipher.EphemeralKeyPair = null;
        }


        [Test]
        public static void TestMessageEncrypt()
        {
            var config = new SocketConfig()
            {
                SessionName = "TEST",
            };
            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();
            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);
            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };
            var storage = new SignalStorage(config.Auth);
            var cipher = new SessionCipher(storage, new ProtocolAddress("27797798179@whatsapp.net"));
            var data = Convert.FromBase64String("+gEwChoyNzc5Nzc5ODE3OUBzLndoYXRzYXBwLm5ldBISMhAKDm9oIGhlbGxvIHRoZXJlAwMD");
            var enc = cipher.Encrypt(data);

            if (enc.Data.ToBase64() == "MwohBZTF9+2FCJ5gK4GVpWGbfsHorSV+Ak5kjXBwol/k3Z1HEAAYACJAXTAtgSv9hL3PuFlqCQX2t4dV9S59gKGnVE3CBzod/6sLf3zM9dd3Z2cJh83rqKUGGkoNa5Q6jSqTxyIzanp63IU0ZCFnNqSf")
            {
                Assert.Pass();
            }
            else
            {

                Assert.Fail();
            }
            enc = cipher.Encrypt(data);
            var session = cipher.GetRecord();
            var currentSession = session.Sessions["BfVT0Dram/Xpa5pBIzH+LbTB3kfro7Y4x3+uhQEIS2sv"];

            if (currentSession.Chains[currentSession.CurrentRatchet.EphemeralKeyPair.Public.ToBase64()].ChainKey.Key.ToBase64() == "1d4d9xbnPgaXS4tckvJNyQ2ijGytNWEUxjoDDkHNHNo=")
            {
                Assert.Pass();
            }
            else
            {

                Assert.Fail();
            }
        }


        [Test]
        public static void TestInitOutgoing()
        {
            var config = new SocketConfig()
            {
                SessionName = "TEST",
            };
            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();
            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);
            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };
            var storage = new SignalStorage(config.Auth);

            var lines = @"Outgoing: 27797798179.17
Be2bRNKMspaCBZbebXcfsFthN07kj3FlvOns/dyHNcFz
BSxNNMB0nIVraSvQ3+00o8Aw6qNJfvp3McNE3rZmb40Q
16591268
1
BXMo39+yxfO9uF6IHoWczHHgqKZ3fkNtnEvLIOmrcrEU
hIN7l2sU/djZWGCp1yb64mfm+kj8HlW4f9qn0af9SAOgoyV51yuQVtmfRigQIZklNVGe0Mp6gwl6oE8K+i5WAg==
BWIb5rB+tzjVPfwoUvJyWTcqyUAoU067jAdgpQRMuXxv
kFQ3siF82p1LwzGBxmAbVINNv4fXlItxU1mmH82061s=
BdWUmSafT6S+K2DBIuc/COjncHSoXm2vyKgJg/UTeThc
COLzV1PCiQLEfrcdUyGq6LBhU8AH8vFElHMyFLm5In4=
Outgoing: 27797798179.17 done";

            var split = lines.Split('\n');

            var inputsess = new E2ESession()
            {
                IdentityKey = Convert.FromBase64String(split[1]),
                PreKey = new PreKeyPair()
                {
                    Public = Convert.FromBase64String(split[2]),
                    KeyId = split[3].ToUInt32(),
                },
                RegistrationId = 569050546,
                SignedPreKey = new SignedPreKey()
                {
                    KeyId = split[4].ToUInt32(),
                    Public = Convert.FromBase64String(split[5]),
                    Signature = Convert.FromBase64String(split[6])
                },
            };

            var baseKey = new KeyPair()
            {
                Public = Convert.FromBase64String(split[7]),
                Private = Convert.FromBase64String(split[8])
            };

            var outKey = new KeyPair()
            {
                Public = Convert.FromBase64String(split[9]),
                Private = Convert.FromBase64String(split[10])
            };


            var sessionBuilder = new SessionBuilder(storage, new ProtocolAddress("27797798179.17@whatsapp.net"));
            SessionBuilder.OutKeyPair = baseKey;
            SessionBuilder.SendKeyPair = outKey;
            var session = sessionBuilder.InitOutGoing(inputsess);
            //Should Match

            var currentSession = session.Sessions["BWIb5rB+tzjVPfwoUvJyWTcqyUAoU067jAdgpQRMuXxv"];
            if (currentSession.PendingPreKey?.BaseKey?.ToBase64() == "BWIb5rB+tzjVPfwoUvJyWTcqyUAoU067jAdgpQRMuXxv")
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
            SessionBuilder.OutKeyPair = null;
            SessionBuilder.SendKeyPair = null;
        }

        [Test]
        public static void TestSendMessageBuffer()
        {
            var json = "{\"tag\":\"iq\",\"attrs\":{\"to\":\"@s.whatsapp.net\",\"type\":\"get\",\"xmlns\":\"usync\",\"id\":\"46858.8451-10\"},\"content\":[{\"tag\":\"usync\",\"attrs\":{\"sid\":\"46858.8451-9\",\"mode\":\"query\",\"last\":\"true\",\"index\":\"0\",\"context\":\"message\"},\"content\":[{\"tag\":\"query\",\"attrs\":{},\"content\":[{\"tag\":\"devices\",\"attrs\":{\"version\":\"2\"}}]},{\"tag\":\"list\",\"attrs\":{},\"content\":[{\"tag\":\"user\",\"attrs\":{\"jid\":\"27665245067@s.whatsapp.net\"}},{\"tag\":\"user\",\"attrs\":{\"jid\":\"27797798179@s.whatsapp.net\"}}]}]}]}";

            var binaryNode = JsonConvert.DeserializeObject<BinaryNode>(json);

            var endode = BufferWriter.EncodeBinaryNode(binaryNode);
            var base64 = endode.ToByteArray().ToBase64();

            Debug.Assert(base64 == "APgKHg76AAMEMRmyCP+HRoWLhFGhD/gB+Ayy/ANzaWT/BkaFi4RRqZGgWs/RT7cL+AL4AqD4AfgDKE0z+AJu+AL4AxAP+v+GJ2ZSRQZ/A/gDEA/6/4YneXeYF58D");



            var iq = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    {"to", "@s.whatsapp.net" },
                    {"type" , "get" },
                    {"xmlns","usync" },
                    {"id","7834.23612-8" }
                },
                content = new BinaryNode[]
    {
                    new BinaryNode()
                    {
                        tag = "usync",
                        attrs =
                        {
                            {"sid", "7834.23612-7" },
                            {"mode","usync" },
                            {"last","true" },
                            {"index","0" },
                            {"context","message" }
                        },
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "query",
                                content = new BinaryNode[]
                                {
                                    new BinaryNode()
                                    {
                                        tag = "devices",
                                        attrs =
                                        {
                                            {"version", "2" }
                                        }
                                    }
                                },
                            },
                            new BinaryNode()
                            {
                                tag = "list",
                                content = new BinaryNode[]
                                {
                                    new BinaryNode()
                                    {
                                        tag = "user",
                                        attrs =
                                        {
                                            {"jid","27665245067@s.whatsapp.net" }
                                        }
                                    },
                                    new BinaryNode()
                                    {
                                        tag = "user",
                                        attrs =
                                        {
                                            {"jid","27797798179@s.whatsapp.net" }
                                        }
                                    }
                                }
                            }
                        }
                    }
    }
            };
            endode = BufferWriter.EncodeBinaryNode(iq);
            var buffer = endode.ToByteArray();
            Assert.Pass();
        }

        [Test]
        public static void GenerateMessages()
        {
            var reply = @"{
  ""key"": {
    ""remoteJid"": ""27797798179@s.whatsapp.net"",
    ""fromMe"": false,
    ""id"": ""FBDBCD0EC4256FA59F2BF8C3B9439002"",
    ""participant"": """"
  },
  ""message"": {
    ""conversation"": ""Hello"",
    ""messageContextInfo"": {
      ""deviceListMetadata"": {
        ""senderKeyHash"": ""qerq+rAf42paLA=="",
        ""senderTimestamp"": ""1711460193"",
        ""recipientKeyHash"": ""H/ksOwnzLPeHdg=="",
        ""recipientTimestamp"": ""1711546000""
      },
      ""deviceListMetadataVersion"": 2
    }
  },
  ""status"": ""SERVER_ACK"",
  ""broadcast"": false,
  ""pushName"": ""Donald Jansen""
}";

            var replyTo = WebMessageInfo.Parser.ParseJson(reply);
            //var options = new MessageGenerationOptionsFromContent()
            //{
            //    Quoted = replyTo
            //};

            //var message = MessageUtil.GenerateWAMessageContent(new TextMessageContent() { Text = "oh hello there" }, options);
            //var wamessage = MessageUtil.GenerateWAMessageFromContent(replyTo.Key.RemoteJid, message, options);

            Assert.Pass();
        }

        [Test]
        public static void TestInflate()
        {
            var buffer = Convert.FromBase64String("eAHjYLHS5pIyMje3NDe3tDA0t3Qo1ivPSCwpTiwo0MtLLRHidcnPS8xJUfBKzCtOzQMAE8oNnQ==");

            var inflat = BufferReader.Inflate(buffer);

            if (Convert.ToBase64String(inflat) == "CAQ6KwoaMjc3OTc3OTgxNzlAcy53aGF0c2FwcC5uZXQSDURvbmFsZCBKYW5zZW4=")
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public static void LtHashTest()
        {
            var ss = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

            var addBuffs = new List<byte[]>
            {
                Convert.FromBase64String("tpAlo9TRdclDonMD7ragdYFjKP5VaXuXokzOtxtM2u8="),
            };
            var subBuffs = new List<byte[]>();

            var ah = new HashAntiTampering("WhatsApp Patch Integrity");

            var result = ah.SubstarctThenAdd(ss, addBuffs, subBuffs);

            Assert.Pass();

        }

        [Test]
        public static void TestSliceMinusEnd()
        {
            var buffer = Convert.FromBase64String("Q9QximJn3r0imIXilMioUt6XLzDAGe4HTQup189OgKvsfu7M5N/g/bHN/5L3m3vGOgqPwaYfGsb4i43bSYo3MzKFKZgkqX0LCISFjQtxK64yo/M3VbO2OJoTfZLMIq89");
            var a = buffer.Slice(0, -32).ToBase64();
            var b = buffer.Slice(-32).ToBase64();

            if (b == "MoUpmCSpfQsIhIWNC3ErrjKj8zdVs7Y4mhN9kswirz0=")
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }


        }


        [Test]
        public static void TestSuccessSign()
        {
            var @private = "KOwFT3vxL5IcJp+wKvIv2gmeHwlQY2V0ZkkmApQZ5GI=";
            var deviceMsg = "BgEI/f7nsAIQoP+3rQYYKCAAKACrRV5Gg5U97GP9ty08k+6gNVYvFnSceP9fsqNiYoGMbLy6K/AKoi08AC8i2wd0pHCLQ6zZSn/PJ6oTDx7DvLBi";
            var deviceSignature = "zqAkA3s+PMr9YV+nKGT8gOojEH4P/Cp0VruJwBJlrH2JJ/nrSmqQ7zhSt1q0qcBvDMqXVItiSyDT3eKBelzkhQ==";

            var newSig = Curve.Sign(Convert.FromBase64String(@private), Convert.FromBase64String(deviceMsg));
            var base64 = Convert.ToBase64String(newSig);

            var reply = "APgIHg76AAMEFAj/hTCIKJVv+AH4AvwQcGFpci1kZXZpY2Utc2lnbvgB+AT8D2RldmljZS1pZGVudGl0efwJa2V5LWluZGV4/wFB/JgKEgj9/uewAhD4h7itBhgpIAAoABpApzrqAU8tQpwaIFVQeg5azu2ZEbAtsNiJlJzEi7MDq1A61gTkVWy6MRrKbiiw6JCG1F0t/PP6BI6kM8hqzQD5BSJAtHkqkFvLxlN78cHNCqZ8UYHC9ZS4xol5fyU5srBbmaPxHULG/MDtyRueqZYel6VN1uCf19HZvkYER2TUlPxUgw==";
            var data = BufferReader.DecodeDecompressedBinaryNode(Convert.FromBase64String(reply));


            var encoded = BufferWriter.EncodeBinaryNode(data).ToByteArray();
            base64 = Convert.ToBase64String(encoded);

            if (base64 == reply)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public static void TestEncodeAndDecode()
        {
            var iq = new BinaryNode()
            {
                tag = "iq",
                attrs = new Dictionary<string, string>()
                {
                    {"to", Constants.S_WHATSAPP_NET },
                    {"type","result" },
                    {"id", "1763566167" }
                }
            };

            //Check Ecoding
            var data = BufferWriter.EncodeBinaryNode(iq).ToByteArray();
            var data2 = Encoding.UTF8.GetString(data);
            var iq2 = BufferReader.DecodeDecompressedBinaryNode(data);


            if (iq.attrs["id"] == iq2.attrs["id"])
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        [Test]
        public static void TestEncryptMediaMessage()
        {
            var img = Convert.FromBase64String("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");
            var mediaKey = Convert.FromBase64String("Sn9R0N0ROTDSOme0S38pTDfQY5jsdvuzamFuFl5/zo0=");

            var mediaKeys = MediaMessageUtil.GetMediaKeys(mediaKey, "image");

            var hmac = new HMACSHA256(mediaKeys.MacKey);
            SHA256 sha256Plain = SHA256.Create();
            SHA256 sha256Enc = SHA256.Create();

            Append(hmac, mediaKeys.IV, false);

            byte[] encrypted = [];
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256;
                aesAlg.Key = mediaKeys.CipherKey;
                aesAlg.IV = mediaKeys.IV;
                aesAlg.Mode = CipherMode.CBC;

                // Create a decryptor to perform the stream transform
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);


                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(img);
                        encrypted = msEncrypt.ToArray();
                        //Plain
                        sha256Plain.TransformFinalBlock(img, 0, img.Length);

                        var size = encrypted.Length;
                        csEncrypt.FlushFinalBlock();
                        encrypted = msEncrypt.ToArray();
                        Append(hmac, encrypted, true);
                        sha256Enc.TransformBlock(encrypted, 0, encrypted.Length, encrypted, 0);
                    }
                }
            }

            var mac = hmac.Hash.Slice(0, 10);

            sha256Enc.TransformFinalBlock(mac, 0, mac.Length);

            var sha256PlainHash = sha256Plain.Hash;
            var fileEncSha256Hash = sha256Enc.Hash;


            var hashed = mac.ToBase64();
            var fileSha256 = sha256PlainHash.ToBase64();
            var fileEncSha256 = fileEncSha256Hash.ToBase64();

            if (hashed == "DWyfkna8ZVxjWg==" &&  fileSha256 == "KTuVFyxDc6mTm4GXPlO3Z911Wd8RBeTrPLSWAEdqW8M=" && fileEncSha256 == "pieMLY6Vf4F2qIEFt2yO6V/mTjiao3T/0XmOis4+53A=")
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }


        public static void Append(HMACSHA256 hmac, byte[] buffer, bool isFinal)
        {
            if (!isFinal)
            {
                hmac.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
            }
            else
            {
                hmac.TransformFinalBlock(buffer, 0, buffer.Length);
            }
        }


        [Test]
        public static void TestGroupMessageEncrypt()
        {
            var config = new SocketConfig()
            {
                SessionName = "TestGroupEnc",
            };
            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }

            var file = Path.Join(config.CacheRoot, "sender-key", "120363264521029662@g.us__27665245067.91.json");
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);
            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };
            var storage = new SignalStorage(config.Auth);

            var repository = new SignalRepository(config.Auth);

            var groupId = "120363264521029662@g.us";
            var meId = "27665245067:91@s.whatsapp.net";
            var data = Convert.FromBase64String("MgcKBUhlbGxvBAQEBA==");

            var result = repository.EncryptGroupMessage(groupId, meId, data);


            var skmsg = result.SenderKeyDistributionMessage.ToBase64() == "Mwi9hOPJBBAAGiCY6wHeNXADNM6OrTwOlcmlYzgtroi+rItJOnNJ5lCMaiIhBetS2PNKwvg7xDFgSg62k0kZFsq3QGb9Fx60+ikazut6";
            var b64 = result.CipherText.ToBase64() == "Mwi9hOPJBBAAGhA2p8UGDXCGJSU/Sf4mrx0jzn9FAB/Ko3IUyebmAYVh8livG2lfReUgS8eLzxvwDpJuPDwOWz1XRea8V+AzrdrOqNii+wNdfF+SUMFq9CDEiA==";
            if (b64 && skmsg)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }
    }
}