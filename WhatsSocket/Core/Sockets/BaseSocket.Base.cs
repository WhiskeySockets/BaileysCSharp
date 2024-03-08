using Proto;
using Google.Protobuf;
using static Proto.ClientPayload.Types;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Sockets;
using WhatsSocket.Core.Events;
using System.Text;
using WhatsSocket.Core.Models;
using WhatsSocket.Exceptions;
using System.Threading;
using System.Diagnostics;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.Utils;
using System.Linq;
using WhatsSocket.Core.WABinary;
using WhatsSocket.Core.Sockets.Client;
using WhatsSocket.Core.Signal;
using static WhatsSocket.Core.Utils.GenericUtils;
using System.Diagnostics.CodeAnalysis;

namespace WhatsSocket.Core
{


    public partial class BaseSocket
    {

        public EventEmitter EV { get; set; }


        Dictionary<string, Func<BinaryNode, Task<bool>>> events = new Dictionary<string, Func<BinaryNode, Task<bool>>>();
        Dictionary<string, TaskCompletionSource<BinaryNode>> waits = new Dictionary<string, TaskCompletionSource<BinaryNode>>();

        private string[] Browser = { "Baileys 2.0", "Chrome", "4.0.0" };

        AbstractSocketClient Client;
        NoiseHandler noise;
        public long Epoch { get; set; }
        public Logger Logger { get; }
        private SignalRepository Repository { get; set; }

        Thread keepAliveThread;
        CancellationTokenSource keepAliveToken;
        DateTime lastReceived;

        KeyPair EphemeralKeyPair { get; set; }
        public string UniqueTagId { get; set; }
        CancellationTokenSource qrTimerToken;

        Dictionary<string, int> MessageRetries = new Dictionary<string, int>();

        public bool SendActiveReceipts { get; set; }

        public AuthenticationCreds? Creds { get; set; }
        public SocketConfig Config { get; }

        public string GenerateMessageTag()
        {
            return $"{UniqueTagId}{Epoch++}";
        }
        private string GenerateMdTagPrefix()
        {
            var bytes = AuthenticationUtils.RandomBytes(4);
            return $"{BitConverter.ToUInt16(bytes)}.{BitConverter.ToUInt16(bytes, 2)}-";
        }

        public BaseSocket([NotNull] SocketConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Config = config;
            EV = new EventEmitter(this);
            Creds = config.Auth.Creds;
            Logger = config.Logger;

            InitStores();
            events["frame"] = OnFrame;
            events["CB:stream:error"] = OnStreamError;
            events["CB:iq,type:set,pair-device"] = OnPairDevice;
            events["CB:xmlstreamend"] = StreamEnd;
            events["CB:iq,,pair-success"] = OnPairSuccess;
            events["CB:success"] = OnSuccess;
            events["CB:failure"] = OnFailure;
            events["CB:ib,,downgrade_webclient"] = DowngradeWebClient;
            events["CB:message"] = OnMessage; ;
            events["CB:call"] = OnCall;
            events["CB:receipt"] = OnReceipt;
            events["CB:notification"] = OnNotification;
            events["CB:ack,class:message"] = OnHandleAck;
        }



        #region Receiving


        private void StartKeepAliveRequest()
        {
            keepAliveToken = new CancellationTokenSource();
            keepAliveThread = new Thread(() => KeepAliveHandler());
            keepAliveThread.Start();
        }


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

                var iq = new BinaryNode("iq")
                {
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
                lastReceived = DateTime.Now;
                Thread.Sleep(keepAliveIntervalMs);
            }
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

        #region Frame Events

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

        private Task<bool> DowngradeWebClient(BinaryNode node)
        {
            return Task.FromResult(true);
        }

        private Task<bool> OnFailure(BinaryNode node)
        {
            if (node.attrs["reason"] == "401")
            {
                Client.Opened -= Client_Opened;
                Client.Disconnected -= Client_Disconnected;

                EV.Emit(DisconnectReason.LoggedOut);
            }

            return Task.FromResult(true);
        }

        private async Task<bool> OnPairDevice(BinaryNode message)
        {
            var iq = new BinaryNode("iq")
            {
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


                    //File.WriteAllBytes("qr.png", data);

                    EV.EmitQR(new QRData(qr));


                    //await Console.Out.WriteLineAsync(qr);
                    //await Console.Out.WriteLineAsync(data);
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
            try
            {

                var reply = ValidateConnectionUtil.ConfigureSuccessfulPairing(Creds, node);

                EV.Emit(Creds);

                Console.Clear();
                SendNode(reply);
            }
            catch (Boom ex)
            {
                End(ex.Message, ex.Reason);
            }
            return Task.FromResult(true);

        }

        private async Task<bool> OnSuccess(BinaryNode node)
        {
            await UploadPreKeysToServerIfRequired();
            await SendPassiveIq("active");

            Logger.Info("opened connection to WA");

            return true;
        }
        private async Task<bool> StreamEnd(BinaryNode node)
        {
            await Task.Yield();
            End("Connection Terminated by Server", DisconnectReason.ConnectionClosed);
            return true;
        }

        #endregion

        #region Sending

        /** send a binary node */
        private void SendNode(BinaryNode iq)
        {
            var buffer = BufferWriter.EncodeBinaryNode(iq).ToByteArray();
            SendRawMessage(buffer);
        }

        private void SendRawMessage(byte[] bytes)
        {
            var toSend = noise.EncodeFrame(bytes);
            Logger.Info(new { bytes = Convert.ToBase64String(toSend) }, $"send {toSend.Length} bytes");
            Client.Send(toSend);
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


        #endregion

        #region Events

        private void Client_MessageRecieved(AbstractSocketClient sender, DataFrame frame)
        {
            noise.DecodeFrame(frame.Buffer, OnFrameDeecoded);
        }

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
        private void Client_Disconnected(AbstractSocketClient sender, DisconnectReason reason)
        {
            Client.Opened -= Client_Opened;
            Client.Disconnected -= Client_Disconnected;

            EV.Emit(reason);
        }
        private async Task<bool> Emit(string key, BinaryNode e)
        {
            if (events.ContainsKey(key))
            {
                return await events[key](e);
            }
            return false;
        }

        #endregion

        #region Login Successfull

        private async Task SendPassiveIq(string tag)
        {
            var iq = new BinaryNode("iq")
            {
                attrs = new Dictionary<string, string>()
                {
                    {"to",Constants.S_WHATSAPP_NET },
                    {"xmlns" ,"passive" },
                    {"type","set" },
                },
                content = new BinaryNode[]
                {
                    new BinaryNode(tag)
                }
            };
            var result = await Query(iq);

        }

        private async Task UploadPreKeysToServerIfRequired()
        {
            var preKeyCount = await GetAvailablePreKeysOnServer();
            Logger.Info($"{preKeyCount} pre-keys found on server");
            if (preKeyCount <= Constants.MIN_PREKEY_COUNT)
            {
                await UploadPreKeys();
            }
        }

        private async Task UploadPreKeys()
        {
            if (Creds == null)
            {
                throw new ArgumentNullException(nameof(Creds));
            }
            var node = ValidateConnectionUtil.GetNextPreKeysNode(Creds, Repository.Storage.Keys, Constants.INITIAL_PREKEY_COUNT);
            var result = await Query(node);
            EV.Emit(Creds);
        }

        private async Task<int> GetAvailablePreKeysOnServer()
        {
            var iq = new BinaryNode("iq")
            {
                attrs = new Dictionary<string, string>()
                {
                    {"id", GenerateMessageTag() },
                    {"type","get" },
                    {"xmlns" ,"encrypt" },
                    {"to",Constants.S_WHATSAPP_NET }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode("count")
                }
            };
            var result = await Query(iq);
            var countChild = GetBinaryNodeChild(result, "count");
            return +(Convert.ToInt32(countChild?.attrs["value"]));
        }

        #endregion

        #region Pairing

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
            if (Config.Mobile)
            {
                //TODO : generateMobileNode
            }
            else if (Creds.Me == null)
            {
                var node = ValidateConnectionUtil.GenerateRegistrationNode(Creds);
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

        #endregion

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


        private NoiseHandler MakeNoiseHandler()
        {
            return new NoiseHandler(EphemeralKeyPair, Logger);
        }

        private void End(string reason, DisconnectReason connectionLost)
        {

            Logger.Trace(new { reason = connectionLost }, reason);

            keepAliveToken?.Cancel();
            qrTimerToken?.Cancel();


            Console.WriteLine($"{reason} - {connectionLost}");
        }

        public void NewAuth()
        {
            Creds = AuthenticationUtils.InitAuthCreds();
            InitStores();
        }

        private void InitStores()
        {
            Repository = Config.MakeSignalRepository(EV);


        }
    }
}
