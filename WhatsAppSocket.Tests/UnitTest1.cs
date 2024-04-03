using Newtonsoft.Json;
using Proto;
using System.Diagnostics;
using System.Text;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Signal;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.WABinary;
using WhatsSocket.LibSignal;

namespace WhatsAppSocket.Tests
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
        public static void TestMessageEncrypt()
        {
            var config = new SocketConfig()
            {
                ID = "TEST",
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
                ID = "TEST",
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
            sessionBuilder.OutKeyPair = baseKey;
            sessionBuilder.GenKeyPair = outKey;
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
            var options = new MessageGenerationOptionsFromContent()
            {
                Quoted = replyTo
            };

            var message = MessageUtil.GenerateWAMessageContent(new ExtendedTextMessageModel() { Text = "oh hello there" }, options);
            var wamessage = MessageUtil.GenerateWAMessageFromContent(replyTo.Key.RemoteJid, message, options);

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


    }
}