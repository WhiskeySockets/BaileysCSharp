using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Proto;
using QRCoder;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WhatsSocket.Core;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Sockets;
using WhatsSocket.Exceptions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Google.Protobuf;
using WhatsSocket.Core.WABinary;
using WhatsSocket.Core.Models.Sending.Media;
using WhatsSocket.Core.Models.Sending.NonMedia;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static WASocket socket;
        static void Main(string[] args)
        {
            Tests.RunTests();


            var config = new SocketConfig()
            {
                ID = "27665245067",
            };

            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            //if (!File.Exists(credsFile)) //Just to check if my creds are generating correctly, but it is working
            //{
            //    credsFile = Path.Join(@"B:\Github\Baileys\baileys_auth_info", "creds.json");
            //}
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

            socket = new WASocket(config);


            var authEvent = socket.EV.On<AuthenticationCreds>(EmitType.Update);
            authEvent.Multi += AuthEvent_OnEmit;

            var connectionEvent = socket.EV.On<ConnectionState>(EmitType.Update);
            connectionEvent.Multi += ConnectionEvent_Emit;


            var messageEvent = socket.EV.On<MessageUpsertModel>(EmitType.Upsert);
            messageEvent.Single += MessageEvent_Single;

            var history = socket.EV.On<MessageHistoryModel>(EmitType.Set);
            history.Multi += History_Emit;

            var presence = socket.EV.On<PresenceModel>(EmitType.Update);
            presence.Multi += Presence_Emit;

            //socket.EV.OnCredsChange += Socket_OnCredentialsChangeArgs;
            //socket.EV.OnDisconnect += EV_OnDisconnect;
            //socket.EV.OnQR += EV_OnQR;
            //socket.EV.OnMessageUpserted += EV_OnMessageUpserted;


            socket.MakeSocket();

            Console.ReadLine();
        }

        static List<WebMessageInfo> messages = new List<WebMessageInfo>();
        private static async void MessageEvent_Single(MessageUpsertModel args)
        {
            //Append is old messages (happens when service was offline)
            if (args.Type == MessageUpsertType.Notify)
            {
                foreach (var msg in args.Messages)
                {
                    if (msg.Key.FromMe == false && msg.Message.ExtendedTextMessage != null && msg.Message.ExtendedTextMessage.Text == "runtests")
                    {
                        var jid = JidUtils.JidDecode(msg.Key.Id);
                        // send a simple text!
                        var standard = await socket.SendMessage(msg.Key.RemoteJid, new TextMessageContent() { 
                            Text = "Hi there from C#" });

                        //send a reply messagge
                        var quoted = await socket.SendMessage(msg.Key.RemoteJid,
                            new TextMessageContent() { Text = "Hi this is a C# reply" },
                            new MessageGenerationOptionsFromContent()
                            {
                                Quoted = msg
                            });


                        // send a mentions message
                        var mentioned = await socket.SendMessage(msg.Key.RemoteJid, new TextMessageContent() { 
                            Text = $"Hi @{jid.User} from C# with mention", 
                            Mentions = [msg.Key.RemoteJid] });

                        // send a contact!
                        var contact = await socket.SendMessage(msg.Key.RemoteJid, new ContactMessageContent()
                        {
                            Contact = new ContactShareModel()
                            {
                                ContactNumber = jid.User,
                                FullName = $"{msg.PushName}",
                                Organization = ""
                            }
                        });

                        // send a location! //48.858221124792756, 2.294466243303683
                        var location = await socket.SendMessage(msg.Key.RemoteJid, new LocationMessageContent()
                        {
                            Location = new Message.Types.LocationMessage()
                            {
                                DegreesLongitude = 48.858221124792756,
                                DegreesLatitude = 2.294466243303683,
                            }
                        });

                        //react
                        var react = await socket.SendMessage(msg.Key.RemoteJid, new ReactMessageContent()
                        {
                            Key = msg.Key,
                            ReactText = "💖"
                        });

                        // Sending image
                        var imageMessage = await socket.SendMessage(msg.Key.RemoteJid, new ImageMessageContent()
                        {
                            Image = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\cat.jpeg", FileMode.Open),
                            Caption = "Cat.jpeg"
                        });

                        // send an audio file
                        var audioMessage = await socket.SendMessage(msg.Key.RemoteJid, new AudioMessageContent()
                        {
                            Audio = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\sonata.mp3", FileMode.Open),
                        });

                        // send an audio file
                        var videoMessage = await socket.SendMessage(msg.Key.RemoteJid, new VideoMessageContent()
                        {
                            Video = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\ma_gif.mp4", FileMode.Open),
                            GifPlayback = true
                        });

                        // send a document file
                        var documentMessage = await socket.SendMessage(msg.Key.RemoteJid, new DocumentMessageContent()
                        {
                            Document = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\file.pdf", FileMode.Open),
                            Mimetype = "application/pdf",
                            FileName = "proposal.pdf",
                        });
                    }
                    messages.Add(msg);
                }
            }
        }

        private static void Presence_Emit(PresenceModel[] args)
        {
            Console.WriteLine(JsonConvert.SerializeObject(args[0], Formatting.Indented));
        }

        private static void History_Emit(MessageHistoryModel[] args)
        {
            messages.AddRange(args[0].Messages);
            var jsons = messages.Select(x => x.ToJson()).ToArray();
            var array = $"[\n{string.Join(",", jsons)}\n]";
            Debug.WriteLine(array);
        }


        private static async void ConnectionEvent_Emit(ConnectionState[] args)
        {
            var connection = args[0];
            Debug.WriteLine(JsonConvert.SerializeObject(connection, Formatting.Indented));
            if (connection.QR != null)
            {
                QRCodeGenerator QrGenerator = new QRCodeGenerator();
                QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(connection.QR, QRCodeGenerator.ECCLevel.L);
                AsciiQRCode qrCode = new AsciiQRCode(QrCodeInfo);
                var data = qrCode.GetGraphic(1);
                Console.WriteLine(data);
            }
            if (connection.Connection == WAConnectionState.Close)
            {
                if (connection.LastDisconnect.Error is Boom boom && boom.Data?.StatusCode != (int)DisconnectReason.LoggedOut)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        socket.MakeSocket();
                    }
                    catch (Exception)
                    {

                    }
                }
                else
                {
                    Console.WriteLine("You are logged out");
                }
            }


            if (connection.Connection == WAConnectionState.Open)
            {
                Console.WriteLine("Now you can send messages");

            }
        }

        public static object locker = new object();
        private static void AuthEvent_OnEmit(AuthenticationCreds[] args)
        {
            lock (locker)
            {
                var credsFile = Path.Join(socket.SocketConfig.CacheRoot, $"creds.json");
                var json = AuthenticationCreds.Serialize(args[0]);
                File.WriteAllText(credsFile, json);
            }
        }





        private static void EV_OnDisconnect(BaseSocket sender, DisconnectReason args)
        {
            if (args != DisconnectReason.LoggedOut)
            {
                sender.MakeSocket();
            }
            else
            {
                Directory.Delete(Path.Join(Directory.GetCurrentDirectory(), "test"), true);
                sender.NewAuth();
                sender.MakeSocket();
            }
        }




    }
}
