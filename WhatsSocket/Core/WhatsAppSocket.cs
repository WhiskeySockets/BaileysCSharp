using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;
using Org.BouncyCastle.Tls;
using Proto;
using Google.Protobuf;
using Org.BouncyCastle.X509;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using static Proto.ClientPayload.Types;
using Newtonsoft.Json.Linq;
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Helper;
using System.Diagnostics;
using WhatsSocket.Core.Sockets;
using System.Xml.Linq;
using WhatsSocket.Core.Credentials;
using WhatsSocket.Core.Events;
using System.Security.Cryptography;
using QRCoder;
using System.Security.Principal;
using Org.BouncyCastle.Asn1.X9;

namespace WhatsSocket.Core
{
    public delegate void CredentialsChangeArgs(WhatsAppSocket sender, AuthenticationCreds authenticationCreds);
    public delegate void Disconnected(WhatsAppSocket sender, DisconnectReason disconnectReason);


    public class WhatsAppSocket
    {
        public event CredentialsChangeArgs OnCredentialsChange;
        public event Disconnected OnDisconnected;

        Dictionary<string, Func<BinaryNode, Task<bool>>> events = new Dictionary<string, Func<BinaryNode, Task<bool>>>();
        Dictionary<string, TaskCompletionSource<BinaryNode>> waits = new Dictionary<string, TaskCompletionSource<BinaryNode>>();

        private string[] Browser = { "Baileys", "Chrome", "4.0.0" };

        AbstractSocketClient Client;
        NoiseHandler noise;
        KeyPair EphemeralKeyPair { get; set; }
        public string UniqueTagId { get; set; }
        public bool IsMobile { get; }
        public long Epoch { get; set; }
        public AuthenticationCreds Creds { get; }
        public Logger Logger { get; }

        public string GenerateMessageTag()
        {
            return $"{UniqueTagId}{Epoch++}";
        }


        public WhatsAppSocket(AuthenticationCreds creds, Logger logger, bool isMobile = false)
        {
            Creds = creds;
            Logger = logger;
            Logger.Level = LogLevel.Verbose;
            IsMobile = isMobile;
            events["CB:stream:error"] = OnStreamError;
            events["frame"] = OnFrame;
            events["CB:iq,type:set,pair-device"] = OnPairDevice;
            events["CB:xmlstreamend"] = StreamEnd;
            events["CB:iq,,pair-success"] = OnPairSuccess;
            events["CB:success"] = OnSuccess;

            //events["CB:iq,,pair-success"] = OnPairSuccess;
            //events["CB:failure"] = OnFailure;

        }

        private async Task<bool> OnSuccess(BinaryNode node)
        {
            await UploadPreKeysToServerIfRequired();
            await SendPassiveIq("active");


            return true;
        }

        private Task SendPassiveIq(string v)
        {
            return Task.CompletedTask;
        }

        private async Task UploadPreKeysToServerIfRequired()
        {
            var preKeyCount = await GetAvailablePreKeysOnServer();
            Logger.Info($"{preKeyCount} pre-keys found on server");
            if (preKeyCount <= 5)
            {
                await UploadPreKeys();
            }
        }

        private Task UploadPreKeys()
        {
            return Task.CompletedTask;
        }

        private async Task<int> GetAvailablePreKeysOnServer()
        {
            var iq = new BinaryNode()
            {
                tag = "iq",
                attrs = new Dictionary<string, string>()
                {
                    {"id", GenerateMessageTag() },
                    {"type","get" },
                    {"xmlns" ,"encrypt" },
                    {"to",Constants.S_WHATSAPP_NET }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "count"
                    }
                }
            };
            var result = await Query(iq);
            var countChild = GetBinaryNodeChild(result, "count");
            return +(Convert.ToInt32(countChild?.attrs["value"]));
        }

        private async Task<bool> StreamEnd(BinaryNode node)
        {
            await Task.Yield();
            End("Connection Terminated by Server", DisconnectReason.ConnectionClosed);
            return true;
        }


        /**
         * Connects to WA servers and performs:
         * - simple queries (no retry mechanism, wait for connection establishment)
         * - listen to messages and emit events
         * - query phone connection
         */
        public void MakeSocket()
        {
            Client = new WebSocketClient();
            Client.Opened += Client_Opened;
            Client.Disconnected += Client_Disconnected;
            Client.MessageRecieved += Client_MessageRecieved;

            /** ephemeral key pair used to encrypt/decrypt communication. Unique for each connection */
            EphemeralKeyPair = EncryptionHelper.GenerateKeyPair();

            /** WA noise protocol wrapper */
            noise = MakeNoiseHandler();


            UniqueTagId = GenerateMdTagPrefix();
            Epoch = 1;

            Client.Connect();
        }

        private void Client_Disconnected(AbstractSocketClient sender, DisconnectReason reason)
        {
            Client.Opened -= Client_Opened;
            Client.Disconnected -= Client_Disconnected;

            OnDisconnected?.Invoke(this, reason);

            //if (reason == DisconnectReason.TimedOut)
            //{
            //    MakeSocket();
            //}
        }

        private NoiseHandler MakeNoiseHandler()
        {
            return new NoiseHandler(EphemeralKeyPair, Logger);
        }

        private string GenerateMdTagPrefix()
        {
            var bytes = AuthenticationUtils.RandomBytes(4);
            return $"{BitConverter.ToUInt16(bytes)}.{BitConverter.ToUInt16(bytes, 2)}-";
        }


        private void SendRawMessage(byte[] bytes)
        {
            var toSend = noise.EncodeFrame(bytes);
            Logger.Info(new { bytes = Convert.ToBase64String(toSend) }, $"send {toSend.Length} bytes");
            Client.Send(toSend);
        }

        #region Receiving

        //Data Received from WA

        private void Client_MessageRecieved(AbstractSocketClient sender, DataFrame frame)
        {
            noise.DecodeFrame(frame.Buffer, OnFrameDeecoded);
        }

        //Binary Node Received from WA
        private async void OnFrameDeecoded(BinaryNode message)
        {
            bool fired = await Emit("frame", message);

            if (!fired)
            {

                if (message.tag != "handshake")
                {
                    if (message.attrs.ContainsKey("id"))
                    {
                        var msgId = message.attrs["id"];
                        /* Check if this is a response to a message we sent */
                        fired = fired || await Emit($"{Constants.DEF_TAG_PREFIX}{msgId}", message);

                    }

                    /* Check if this is a response to a message we are expecting */
                    var l0 = message.tag;
                    var l1 = message.attrs;

                    var l2 = "";
                    if (message.content is BinaryNode[] children)
                    {
                        l2 = children[0].tag;
                    }

                    foreach (var item in l1)
                    {
                        fired = fired || await Emit($"{Constants.DEF_CALLBACK_PREFIX}{l0},{item.Key}:{l1[item.Key]},{l2}", message);
                        fired = fired || await Emit($"{Constants.DEF_CALLBACK_PREFIX}{l0},{item.Key}:{l1[item.Key]}", message);
                        fired = fired || await Emit($"{Constants.DEF_CALLBACK_PREFIX}{l0},{item.Key}", message);
                    }
                    fired = fired || await Emit($"{Constants.DEF_CALLBACK_PREFIX}{l0},,{l2}", message) || fired;
                    fired = fired || await Emit($"{Constants.DEF_CALLBACK_PREFIX}{l0}", message) || fired;

                }

            }
        }

        #endregion



        private void Client_Opened(AbstractSocketClient sender)
        {
            try
            {
                ValidateConnection();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in validating connection");
            }
        }


        /** connection handshake */
        public async void ValidateConnection()
        {
            var helloMsg = new HandshakeMessage()
            {
                ClientHello = new HandshakeMessage.Types.ClientHello()
                {
                    Ephemeral = EphemeralKeyPair.Public.ToByteString(),
                }
            };

            Logger.Info(new { Browser, helloMsg }, "connect to WA");

            var init = helloMsg.ToByteArray();

            var result = await NextMessage(init);

            var handshake = HandshakeMessage.Parser.ParseFrom(result);

            Logger.Trace(new { handshake }, "handshake recv from WA");

            var KeyEnc = noise.ProcessHandShake(handshake, Creds.NoiseKey);


            var clientFinish = new HandshakeMessage();
            if (IsMobile)
            {
                //TODO : generateMobileNode
            }
            else if (Creds.Me == null)
            {
                var node = GenerateRegistrationNode(Creds);
                var buffer = node.ToByteArray();
                var payloadEnc = noise.Encrypt(buffer);
                clientFinish.ClientFinish = new HandshakeMessage.Types.ClientFinish()
                {
                    Static = KeyEnc.ToByteString(),
                    Payload = payloadEnc.ToByteString()
                };
                Logger.Info(new { node }, "not logged in, attempting registration...");
            }
            else
            {
                var jid = Creds.Me.ID.Split("@")[0];
                var userDevice = jid.Split(":");

                var user = Convert.ToUInt64(userDevice[0]);
                var device = Convert.ToUInt32(userDevice[1]);
                var node = new ClientPayload()
                {
                    ConnectReason = ConnectReason.UserActivated,
                    ConnectType = ConnectType.WifiUnknown,
                    UserAgent = new UserAgent()
                    {
                        AppVersion = new UserAgent.Types.AppVersion()
                        {
                            Primary = 2,
                            Secondary = 2329,
                            Tertiary = 9,
                        },
                        Platform = UserAgent.Types.Platform.Macos,
                        ReleaseChannel = UserAgent.Types.ReleaseChannel.Release,
                        Mcc = "000",
                        Mnc = "000",
                        OsVersion = "0.1",
                        Manufacturer = "",
                        Device = "Dekstop",
                        OsBuildNumber = "0.1",
                        LocaleLanguageIso6391 = "en",
                        LocaleCountryIso31661Alpha2 = "us",
                    },
                    Passive = true,
                    Username = user,
                    Device = device,

                };
                var buffer = node.ToByteArray();
                var payloadEnc = noise.Encrypt(buffer);
                clientFinish.ClientFinish = new HandshakeMessage.Types.ClientFinish()
                {
                    Static = KeyEnc.ToByteString(),
                    Payload = payloadEnc.ToByteString()
                };
                //TODO : generateLoginNode
                Logger.Info(new { }, "logging in");
            }

            SendRawMessage(clientFinish.ToByteArray());
            noise.FinishInit();

            //TODO: Uncomment below
            StartKeepAliveRequest();
        }

        private void StartKeepAliveRequest()
        {
            keepAliveToken = new CancellationTokenSource();
            keepAliveThread = new Thread(() => KeepAliveHandler());
            keepAliveThread.Start();
        }

        private async Task<bool> Emit(string key, BinaryNode e)
        {
            if (events.ContainsKey(key))
            {
                return await events[key](e);
            }
            return false;
        }

        private async Task<bool> OnStreamError(BinaryNode node)
        {
            await Task.Yield();

            if (node.content is BinaryNode[] nodes)
            {
                node = nodes[0];
            }

            Logger.Error("stream errored out - " + node.tag);

            return true;
        }


        private async Task<bool> OnFrame(BinaryNode message)
        {
            await Task.Yield();

            //For a Query
            if (message.attrs.ContainsKey("id"))
            {
                if (waits.ContainsKey(message.attrs["id"]))
                {
                    waits[message.attrs["id"]].SetResult(message);
                    waits.Remove(message.attrs["id"]);
                    return true;
                }
            }

            //For the Handshake
            if (message.tag == "handshake")
            {
                if (waits.ContainsKey(message.tag))
                {
                    waits[message.tag].SetResult(message);
                    waits.Remove(message.tag);
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> OnPairDevice(BinaryNode message)
        {
            var iq = new BinaryNode()
            {
                tag = "iq",
                attrs = new Dictionary<string, string>()
                {
                    {"to",Constants.S_WHATSAPP_NET },
                    {"type","result" },
                    {"id", message.attrs["id"] }
                },
            };

            SendNode(iq);

            var pairDeviceNode = GetBinaryNodeChild(message, "pair-device");
            var refNodes = new Queue<BinaryNode>(GetBinaryNodeChildren(pairDeviceNode, "ref"));
            var noiseKeyB64 = Creds.NoiseKey.Public.ToBase64();
            var identityKeyB64 = Creds.SignedIdentityKey.Public.ToBase64();
            var advB64 = Creds.AdvSecretKey;

            var qrTimeout = 60000;
            qrTimerToken = new CancellationTokenSource();
            while (!qrTimerToken.IsCancellationRequested)
            {
                if (!Client.IsConnected)
                    return true;

                if (refNodes.TryDequeue(out var refNode))
                {
                    var @ref = Encoding.UTF8.GetString(refNode.ToByteArray());
                    var qr = string.Join(",", @ref, noiseKeyB64, identityKeyB64, advB64);



                    QRCodeGenerator QrGenerator = new QRCodeGenerator();
                    QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(qr, QRCodeGenerator.ECCLevel.L);
                    AsciiQRCode qrCode = new AsciiQRCode(QrCodeInfo);
                    var data = qrCode.GetGraphic(1);
                    //File.WriteAllBytes("qr.png", data);




                    await Console.Out.WriteLineAsync(qr);
                    await Console.Out.WriteLineAsync(data);
                }
                else
                {
                    End("QR refs attempts ended", DisconnectReason.TimedOut);
                    return true;
                }
                try
                {
                    await Task.Delay(qrTimeout, qrTimerToken.Token);
                    qrTimeout = 20000;
                }
                catch (TaskCanceledException)
                {
                    //Ignore
                }
            }
            return true;
        }



        private Task<bool> OnPairSuccess(BinaryNode node)
        {


            var reply = ConfigureSuccessfulPairing(node);


            Console.Clear();
            SendNode(reply);

            return Task.FromResult(true);
        }

        private BinaryNode ConfigureSuccessfulPairing(BinaryNode node)
        {

            var signedIdentityKey = Creds.SignedIdentityKey;

            var msgId = node.attrs["id"].ToString();
            var pairSuccessNode = GetBinaryNodeChild(node, "pair-success");


            var deviceIdentityNode = GetBinaryNodeChild(pairSuccessNode, "device-identity");
            var platformNode = GetBinaryNodeChild(pairSuccessNode, "platform");
            var deviceNode = GetBinaryNodeChild(pairSuccessNode, "device");
            var businessNode = GetBinaryNodeChild(pairSuccessNode, "biz");

            var bizName = businessNode?.attrs["name"];
            var jid = deviceNode.attrs["jid"];

            var detailsHmac = ADVSignedDeviceIdentityHMAC.Parser.ParseFrom(deviceIdentityNode.ToByteArray());


            var advSign = EncryptionHelper.HmacSign(detailsHmac.Details.ToByteArray(), Convert.FromBase64String(Creds.AdvSecretKey));

            // check HMAC matches
            var hmac = detailsHmac.Hmac.ToBase64();
            if (hmac != advSign)
            {
                End("Invalid Account Signature", DisconnectReason.BadSession);
            }

            var account = ADVSignedDeviceIdentity.Parser.ParseFrom(detailsHmac.Details);

            var accountMsg = new byte[] { 6, 0 }
            .Concat(account.Details.ToByteArray())
            .Concat(signedIdentityKey.Public).ToArray();
            if (!EncryptionHelper.Verify(account.AccountSignatureKey.ToByteArray(), accountMsg, account.AccountSignature.ToByteArray()))
            {
                End("Failed to verify account Signature", DisconnectReason.BadSession);
            }

            // sign the details with our identity key
            var deviceMsg = new byte[] { 6, 1 }
            .Concat(account.Details.ToByteArray())
            .Concat(signedIdentityKey.Public)
            .Concat(account.AccountSignatureKey).ToArray();
            account.DeviceSignature = EncryptionHelper.Sign(signedIdentityKey.Private, deviceMsg).ToByteString();

            //TODO: Finish 
            var identity = CreateSignalIdentity(jid, account.AccountSignatureKey);
            var accountEnc = EncodeSignedDeviceIdentity(account, false);


            var deviceIdentity = ADVDeviceIdentity.Parser.ParseFrom(account.Details);

            var reply = new BinaryNode()
            {
                tag = "iq",
                attrs = new Dictionary<string, string>()
                    {
                        {"to",Constants.S_WHATSAPP_NET },
                        {"type","result" },
                        {"id", msgId }
                    },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "pair-device-sign",
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "device-identity",
                                attrs = new Dictionary<string, string>()
                                {
                                    {"key-index",deviceIdentity.KeyIndex.ToString() }

                                },
                                content = accountEnc
                            }
                        },
                    }
                }
            };

            Creds.SignalIdentities = new SignalIdentity[] { identity };
            Creds.Platform = platformNode.attrs["name"];
            Creds.Me = new Contact()
            {
                ID = jid,
                Name = bizName
            };

            OnCredentialsChange?.Invoke(this, Creds);

            return reply;
        }

        private byte[] EncodeSignedDeviceIdentity(ADVSignedDeviceIdentity account, bool includeSignatureKey)
        {
            var clone = ADVSignedDeviceIdentity.Parser.ParseFrom(account.ToByteArray());

            if (!includeSignatureKey)
            {
                clone.ClearAccountSignatureKey();
            }
            return clone.ToByteArray();
        }

        private SignalIdentity CreateSignalIdentity(string jid, ByteString accountSignatureKey)
        {
            return new SignalIdentity()
            {
                Identifier = new ProtocolAddress { Name = jid },
                IdentifierKey = AuthenticationUtils.GenerateSignalPubKey(accountSignatureKey.ToByteArray())
            };
        }

        private BinaryNode[] GetBinaryNodeChildren(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.Where(x => x.tag == tag).ToArray();
            }
            return new BinaryNode[0];
        }

        private BinaryNode? GetBinaryNodeChild(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.FirstOrDefault(x => x.tag == tag);
            }
            return null;
        }

        /** send a binary node */
        private void SendNode(BinaryNode iq)
        {
            var buffer = BufferWriter.EncodeBinaryNode(iq).ToByteArray();
            SendRawMessage(buffer);
        }


        Thread keepAliveThread;
        CancellationTokenSource keepAliveToken;
        DateTime lastReceived;

        CancellationTokenSource qrTimerToken;

        private async void KeepAliveHandler()
        {
            lastReceived = DateTime.Now;
            var keepAliveIntervalMs = 30000;
            Thread.Sleep(keepAliveIntervalMs);
            while (!keepAliveToken.IsCancellationRequested)
            {
                var diff = DateTime.Now - lastReceived;
                if (diff.TotalMilliseconds > keepAliveIntervalMs + 5000)
                {
                    End("Connection was lost", DisconnectReason.ConnectionLost);
                    continue;
                }

                var iq = new BinaryNode()
                {
                    tag = "iq",
                    attrs = new Dictionary<string, string>()
                    {
                        {"id", GenerateMessageTag() },
                        {"to",Constants.S_WHATSAPP_NET },
                        {"type","get" },
                        {"xmlns" ,"w:p" }
                    },
                    content = new BinaryNode[]
                    {
                        new BinaryNode()
                        {
                            tag = "ping"
                        }
                    }
                };



                var result = await Query(iq);

                Thread.Sleep(keepAliveIntervalMs);
            }
        }

        private Task<BinaryNode> Query(BinaryNode iq)
        {
            if (!iq.attrs.ContainsKey("id"))
            {
                iq.attrs["id"] = GenerateMessageTag();
            }
            waits[iq.attrs["id"]] = new TaskCompletionSource<BinaryNode>();
            SendNode(iq);
            return waits[iq.attrs["id"]].Task;
        }

        private void End(string reason, DisconnectReason connectionLost)
        {

            Logger.Trace(new { reason = connectionLost }, reason);

            keepAliveToken?.Cancel();
            qrTimerToken?.Cancel();


            Console.WriteLine($"{reason} - {connectionLost}");
        }

        private ClientPayload GenerateRegistrationNode(AuthenticationCreds creds)
        {
            var appVersion = EncryptionHelper.Md5("2.2329.9");
            var companion = new DeviceProps()
            {
                Os = "Baileys",
                PlatformType = DeviceProps.Types.PlatformType.Chrome,
                RequireFullSync = false,
            };
            var payload = new ClientPayload
            {
                Passive = false,
                ConnectReason = ConnectReason.UserActivated,
                ConnectType = ConnectType.WifiUnknown,
                UserAgent = new UserAgent()
                {
                    AppVersion = new UserAgent.Types.AppVersion()
                    {
                        Primary = 2,
                        Secondary = 2329,
                        Tertiary = 9,
                    },
                    Platform = UserAgent.Types.Platform.Macos,
                    ReleaseChannel = UserAgent.Types.ReleaseChannel.Release,
                    Mcc = "000",
                    Mnc = "000",
                    OsVersion = "0.1",
                    Manufacturer = "",
                    Device = "Dekstop",
                    OsBuildNumber = "0.1",
                    LocaleLanguageIso6391 = "en",
                    LocaleCountryIso31661Alpha2 = "us",
                    //PhoneId = creds.PhoneId
                },
                DevicePairingData = new DevicePairingRegistrationData()
                {
                    BuildHash = appVersion.ToByteString(),
                    DeviceProps = companion.ToByteArray().ToByteString(),
                    ERegid = creds.RegistrationId.EncodeBigEndian().ToByteString(),
                    EKeytype = Constants.KEY_BUNDLE_TYPE.ToByteString(),
                    EIdent = creds.SignedIdentityKey.Public.ToByteString(),
                    ESkeyId = creds.SignedPreKey.KeyId.EncodeBigEndian(3).ToByteString(),
                    ESkeyVal = creds.SignedPreKey.KeyPair.Public.ToByteString(),
                    ESkeySig = creds.SignedPreKey.Signature.ToByteString(),
                }
            };

            return payload;
        }


        public async Task<byte[]> NextMessage(byte[] bytes)
        {
            if (!Client.IsConnected)
            {
                throw new Exception("Connection Closed");
            }
            waits["handshake"] = new TaskCompletionSource<BinaryNode>();
            SendRawMessage(bytes);
            var message = await waits["handshake"].Task;
            return message.ToByteArray();
        }

    }
}
