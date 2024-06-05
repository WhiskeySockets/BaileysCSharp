using Proto;
using Google.Protobuf;
using static Proto.ClientPayload.Types;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Sockets;
using BaileysCSharp.Core.Events;
using System.Text;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Exceptions;
using System.Threading;
using System.Diagnostics;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.Utils;
using System.Linq;
using BaileysCSharp.Core.WABinary;
using BaileysCSharp.Core.Sockets.Client;
using BaileysCSharp.Core.Signal;
using System.Diagnostics.CodeAnalysis;
using BaileysCSharp.Core.NoSQL;
using static BaileysCSharp.Core.Utils.ProcessMessageUtil;
using static BaileysCSharp.Core.WABinary.Constants;
using static BaileysCSharp.Core.Utils.GenericUtils;
using BaileysCSharp.Core.Extensions;
using BaileysCSharp.LibSignal;
using BaileysCSharp.Core.Types;
using System.Collections.Concurrent;
using BaileysCSharp.Core.Logging;

namespace BaileysCSharp.Core
{


    public abstract class BaseSocket
    {
        protected ConcurrentDictionary<string, TaskCompletionSource<BinaryNode>> waits = new ConcurrentDictionary<string, TaskCompletionSource<BinaryNode>>();

        private string[] Browser = ["Ubuntu", "Chrome", "20.0.04",];
        protected AbstractSocketClient WS;
        private NoiseHandler noise;
        private CancellationTokenSource qrTimerToken;
        public Thread keepAliveThread;
        public CancellationTokenSource keepAliveToken;
        public DateTime lastReceived;
        public KeyPair EphemeralKeyPair { get; set; }

        protected Dictionary<string, int> MessageRetries = new Dictionary<string, int>();
        protected Dictionary<string, Func<BinaryNode, Task<bool>>> events = new Dictionary<string, Func<BinaryNode, Task<bool>>>();


        protected SignalRepository Repository { get; set; }
        protected MemoryStore Store { get; set; }

        public string UniqueTagId { get; set; }
        public long Epoch { get; set; }
        public bool SendActiveReceipts { get; set; }
        private bool DidStartBuffer { get; set; }

        public DefaultLogger Logger { get; }
        public EventEmitter EV { get; set; }
        public AuthenticationCreds? Creds { get; set; }
        public SocketConfig SocketConfig { get; }
        public BaseKeyStore Keys { get; }




        public string GenerateMessageTag()
        {
            return $"{UniqueTagId}{Epoch++}";
        }

        private string GenerateMdTagPrefix()
        {
            var bytes = KeyHelper.RandomBytes(4);
            return $"{BitConverter.ToUInt16(bytes)}.{BitConverter.ToUInt16(bytes, 2)}-";
        }

        public BaseSocket([NotNull] SocketConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            SocketConfig = config;
            EV = new EventEmitter(config.Logger);
            Creds = config.Auth.Creds;
            Keys = config.Auth.Keys;
            Logger = config.Logger;

            WS = new WebSocketClient(this);
            InitStores();
            events["frame"] = OnFrame;
            events["CB:xmlstreamend"] = StreamEnd;
            events["CB:iq,type:set,pair-device"] = OnPairDevice;
            events["CB:iq,,pair-success"] = OnPairSuccess;
            events["CB:success"] = OnSuccess;
            events["CB:stream:error"] = OnStreamError;
            events["CB:failure"] = OnFailure;
            events["CB:ib,,downgrade_webclient"] = DowngradeWebClient;
            events["CB:ib,,edge_routing"] = EdgeRouting;
            events["CB:ib,,offline"] = HandleOfflineSynceDone;


        }

        private Task<bool> EdgeRouting(BinaryNode node)
        {
            var edgeRoutingNode = GetBinaryNodeChild(node, "edge_routing");
            var routingInfo = GetBinaryNodeChild(edgeRoutingNode, "routing_info");
            if (routingInfo?.content != null)
            {
                Creds.RoutingInfo = routingInfo.ToByteArray();
            }
            return Task.FromResult(true);
        }

        private Task<bool> HandleOfflineSynceDone(BinaryNode node)
        {
            var child = GetBinaryNodeChild(node, "offline");
            var offlineNotifs = child?.getattr("count").ToUInt32();
            Logger.Info($"handled {offlineNotifs} offline messages/notifications");

            if (DidStartBuffer)
            {
                EV.Flush();
                Logger.Trace("flushed events for initial buffer");
            }

            EV.Emit(EmitType.Update, new ConnectionState() { ReceivedPendingNotifications = true });
            return Task.FromResult(true);
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
                /*
                    check if it's been a suspicious amount of time since the server responded with our last seen
                    it could be that the network is down
                */
                if (diff.TotalMilliseconds > keepAliveIntervalMs + 5000)
                {
                    End("Connection was lost", DisconnectReason.ConnectionLost);
                    continue;
                }

                try
                {
                    // if its all good, send a keep alive request
                    var result = await Query(new BinaryNode("iq")
                    {
                        attrs = new Dictionary<string, string>()
                    {
                        {"id", GenerateMessageTag() },
                        {"to",S_WHATSAPP_NET },
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
                    });
                    lastReceived = DateTime.Now;
                    Thread.Sleep(keepAliveIntervalMs);
                }
                catch (Exception ex)
                {
                    Logger.Error(new { trace = ex.StackTrace }, "error in sending keep alive");
                }
            }
        }

        private async void OnFrameDeecoded(BinaryNode frame)
        {
            bool anyTriggered = await Emit("frame", frame);


            if (frame.tag != "handshake")
            {
                var msgId = frame.getattr("id");


                if (Logger.Level == LogLevel.Trace)
                {
                    Logger.Trace(new { xml = frame }, "recv send");
                }

                /* Check if this is a response to a message we sent */
                anyTriggered = anyTriggered || await Emit($"{DEF_TAG_PREFIX}{msgId}", frame);


                /* Check if this is a response to a message we are expecting */
                var l0 = frame.tag;
                var l1 = frame.attrs;

                var l2 = "";
                if (frame.content is BinaryNode[] children)
                {
                    l2 = children[0].tag;
                }

                foreach (var item in l1)
                {
                    anyTriggered = anyTriggered || await Emit($"{DEF_CALLBACK_PREFIX}{l0},{item.Key}:{l1[item.Key]},{l2}", frame);
                    anyTriggered = anyTriggered || await Emit($"{DEF_CALLBACK_PREFIX}{l0},{item.Key}:{l1[item.Key]}", frame);
                    anyTriggered = anyTriggered || await Emit($"{DEF_CALLBACK_PREFIX}{l0},{item.Key}", frame);
                }
                anyTriggered = anyTriggered || await Emit($"{DEF_CALLBACK_PREFIX}{l0},,{l2}", frame) || anyTriggered;
                anyTriggered = anyTriggered || await Emit($"{DEF_CALLBACK_PREFIX}{l0}", frame) || anyTriggered;

                if (!anyTriggered && Logger.Level == LogLevel.Debug)
                {
                    Logger.Debug(new { unhandled = true, msgId, fromMe = false, frame }, "communication recv");
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
                    if (waits.TryRemove(message.attrs["id"], out var value))
                    {
                        value.SetResult(message);
                    }
                    return true;
                }
            }

            //For the Handshake
            if (message.tag == "handshake")
            {
                if (waits.ContainsKey(message.tag))
                {
                    if (waits.TryRemove(message.tag, out var value))
                    {
                        value.SetResult(message);
                    }
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

        private Task<bool> OnFailure(BinaryNode node)
        {
            var reason = node.getattr("reason") ?? "500";
            if (node.attrs["reason"] == "401")
            {
                WS.Opened -= Client_Opened;
                WS.Disconnected -= Client_Disconnected;
                WS.MessageRecieved -= Client_MessageRecieved;

            }

            End(new Boom("Connection Failure", new BoomData(Convert.ToInt32(reason), node.attrs)));

            return Task.FromResult(true);
        }
        private Task<bool> DowngradeWebClient(BinaryNode node)
        {
            End(new Boom("Multi-device beta not joined", new BoomData(411, node.attrs)));
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
                if (!WS.IsConnected)
                    return true;

                if (refNodes.TryDequeue(out var refNode))
                {
                    var @ref = Encoding.UTF8.GetString(refNode.ToByteArray());
                    var qr = string.Join(",", @ref, noiseKeyB64, identityKeyB64, advB64);


                    EV.Emit(EmitType.Update, new ConnectionState() { QR = qr });


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
            Logger.Debug("pair success recv");
            try
            {

                var reply = ValidateConnectionUtil.ConfigureSuccessfulPairing(Creds, node);


                Logger.Info(Creds, "pairing configured successfully, expect to restart the connection...");

                EV.Emit(EmitType.Update, Creds);

                EV.Emit(EmitType.Update, new ConnectionState() { QR = null, IsNewLogin = true });

                SendNode(reply);
            }
            catch (Boom error)
            {
                Logger.Info(new { trace = error.StackTrace }, "error in pairing");
                End(error);
            }
            return Task.FromResult(true);

        }

        private async Task<bool> OnSuccess(BinaryNode node)
        {
            await UploadPreKeysToServerIfRequired();
            await SendPassiveIq("active");

            Logger.Info("opened connection to WA");
            EV.Emit(EmitType.Update, new ConnectionState() { Connection = WAConnectionState.Open });

            return true;
        }
        private async Task<bool> StreamEnd(BinaryNode node)
        {
            await Task.Yield();
            End(new Boom("Connection Terminated by Server", new BoomData(DisconnectReason.ConnectionLost)));
            return true;
        }

        #endregion

        #region Sending

        /** send a binary node */
        protected void SendNode(BinaryNode frame)
        {
            if (Logger.Level == LogLevel.Trace)
            {
                Logger.Trace(new { xml = frame }, "xml send");
            }

            var buffer = BufferWriter.EncodeBinaryNode(frame).ToByteArray();
            SendRawMessage(buffer);
        }


        protected void SendRawMessage(byte[] bytes)
        {
            var toSend = noise.EncodeFrame(bytes);
            Logger.Raw(new { bytes = Convert.ToBase64String(toSend) }, $"send {toSend.Length} bytes");
            WS.Send(toSend);
        }

        protected Task<BinaryNode> Query(BinaryNode iq)
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
            if (!WS.IsConnected)
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
            noise.DecodeFrameNew(frame.Buffer, OnFrameDeecoded);
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
            WS.Opened -= Client_Opened;
            WS.Disconnected -= Client_Disconnected;
            WS.MessageRecieved -= Client_MessageRecieved;

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

        protected async Task UploadPreKeys()
        {
            if (Creds == null)
            {
                throw new ArgumentNullException(nameof(Creds));
            }
            var node = ValidateConnectionUtil.GetNextPreKeysNode(Creds, Keys, Constants.INITIAL_PREKEY_COUNT);
            await Query(node);

            EV.Emit(EmitType.Update, Creds);
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


            ClientPayload node;
            var clientFinish = new HandshakeMessage();
            if (Creds.Me == null)
            {
                node = ValidateConnectionUtil.GenerateRegistrationNode(Creds, SocketConfig);
                Logger.Info(new { node }, "not logged in, attempting registration...");
            }
            else
            {
                node = ValidateConnectionUtil.GenerateLoginNode(Creds.Me.ID, SocketConfig);
                Logger.Info(new { }, "logging in");
            }


            var payloadEnc = noise.Encrypt(node.ToByteArray());

            clientFinish.ClientFinish = new HandshakeMessage.Types.ClientFinish()
            {
                Static = KeyEnc.ToByteString(),
                Payload = payloadEnc.ToByteString()
            };

            SendRawMessage(clientFinish.ToByteArray());
            noise.FinishInit();

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
            WS.MakeSocket();
            WS.Opened += Client_Opened;
            WS.Disconnected += Client_Disconnected;
            WS.MessageRecieved += Client_MessageRecieved;

            /** ephemeral key pair used to encrypt/decrypt communication. Unique for each connection */
            EphemeralKeyPair = Curve.GenerateKeyPair();

            /** WA noise protocol wrapper */
            noise = MakeNoiseHandler();


            UniqueTagId = GenerateMdTagPrefix();
            Epoch = 1;

            BeforeConnect();
            WS.Connect();
            closed = false;
        }

        bool closed = false;
        private void BeforeConnect()
        {
            if (Creds.Me != null)
            {
                // start buffering important events
                // if we're logged in
                EV.Buffer();
                DidStartBuffer = true;
            }
            EV.Emit(EmitType.Upsert, new ConnectionState() { Connection = WAConnectionState.Connecting, ReceivedPendingNotifications = false, QR = null });
        }

        private NoiseHandler MakeNoiseHandler()
        {
            return new NoiseHandler(EphemeralKeyPair, Logger);
        }


        public void OnUnexpectedError(Exception error, string message)
        {
            Logger.Error(error, $"unexpected error in '{message}'");
        }

        private void End(Boom error)
        {
            if (closed)
            {
                Logger.Trace(new { trace = error.StackTrace }, "connection already closed");
                return;
            }

            Logger.Trace(new { trace = error?.StackTrace }, error != null ? "connection errored" : "connection closed");
            keepAliveToken?.Cancel();
            qrTimerToken?.Cancel();

            //try
            //{
            //    Client.Disconnect();
            //}
            //catch (Exception)
            //{
            //}

            closed = true;

            EV.Emit(EmitType.Update, new ConnectionState()
            {
                Connection = WAConnectionState.Close,
                LastDisconnect = new LastDisconnect()
                {
                    Date = DateTime.Now,
                    Error = error
                }
            });
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
            Repository = SocketConfig.MakeSignalRepository(EV);
            Store = SocketConfig.MakeStore(EV, Logger);

        }
    }
}
