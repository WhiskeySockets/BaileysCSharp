using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Proto;
using QRCoder;
using System.Buffers;
using System.Diagnostics;
using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Extensions;
using BaileysCSharp.Core.Sockets;
using BaileysCSharp.Exceptions;
using BaileysCSharp.Core.Models.Sending.Media;
using BaileysCSharp.Core.Models.Sending.NonMedia;
using BaileysCSharp.Core.Models.Sending;
using BaileysCSharp.Core.Types;
using BaileysCSharp.Core.Utils;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static List<WebMessageInfo> messages = new List<WebMessageInfo>();
        static WASocket socket;
        public static object locker = new object();

        static void Main(string[] args)
        {
            Tests.RunTests();


            var config = new SocketConfig()
            {
                SessionName = "27665245067",
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

            socket = new WASocket(config);


            socket.EV.Auth.Update += Auth_Update;
            socket.EV.Connection.Update += Connection_Update;
            socket.EV.Message.Upsert += Message_Upsert;
            socket.EV.MessageHistory.Set += MessageHistory_Set;
            socket.EV.Pressence.Update += Pressence_Update;

            socket.MakeSocket();

            Console.ReadLine();
        }

        private static void Pressence_Update(object? sender, PresenceModel e)
        {
            Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.Indented));
        }

        private static void MessageHistory_Set(object? sender, MessageHistoryModel[] e)
        {
            messages.AddRange(e[0].Messages);
            var jsons = messages.Select(x => x.ToJson()).ToArray();
            var array = $"[\n{string.Join(",", jsons)}\n]";
            Debug.WriteLine(array);
        }

        private static async void Message_Upsert(object? sender, MessageEventModel e)
        {
            //offline messages synced
            if (e.Type == MessageEventType.Append)
            {

            }

            //new messages
            if (e.Type == MessageEventType.Notify)
            {
                foreach (var msg in e.Messages)
                {
                    if (msg.Message == null)
                        continue;

                    if (msg.Message.ExtendedTextMessage == null)
                        continue;

                    if (msg.Key.FromMe == false && msg.Message.ExtendedTextMessage != null && msg.Message.ExtendedTextMessage.Text == "runtests")
                    {
                        var jid = JidUtils.JidDecode(msg.Key.Id);
                        // send a simple text!
                        var standard = await socket.SendMessage(msg.Key.RemoteJid, new TextMessageContent()
                        {
                            Text = "Hi there from C#"
                        });

                        //send a reply messagge
                        var quoted = await socket.SendMessage(msg.Key.RemoteJid,
                            new TextMessageContent() { Text = "Hi this is a C# reply" },
                            new MessageGenerationOptionsFromContent()
                            {
                                Quoted = msg
                            });


                        // send a mentions message
                        var mentioned = await socket.SendMessage(msg.Key.RemoteJid, new TextMessageContent()
                        {
                            Text = $"Hi @{jid.User} from C# with mention",
                            Mentions = [msg.Key.RemoteJid]
                        });

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

        private static async void Connection_Update(object? sender, ConnectionState e)
        {
            var connection = e;
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
                var groupId = "@g.us";
                var chatId = "@s.whatsapp.net";
                var chatId2 = "@s.whatsapp.net";

                Console.WriteLine("Now you can send messages");

                var standard = await socket.SendMessage(chatId, new TextMessageContent()
                {
                    Text = "Hi *there* from C#"
                });

                //var group = await socket.GroupCreate("Test", [chatId]);
                //await socket.GroupUpdateSubject(groupId, "Subject Nice");
                //await socket.GroupUpdateDescription(groupId, "Description Nice");


                // send a simple text!
                //var standard = await socket.SendMessage(groupId, new TextMessageContent()
                //{
                //    Text = "Hi there from C#"
                //});


                //await socket.GroupSettingUpdate(groupId, GroupSetting.Not_Announcement);

                //await socket.GroupMemberAddMode(groupId, MemberAddMode.All_Member_Add); 

                //await socket.GroupParticipantsUpdate(groupId, [chatId2], ParticipantAction.Promote);
                //await socket.GroupParticipantsUpdate(groupId, [chatId2], ParticipantAction.Demote);

                //var result = await socket.GroupInviteCode(groupId);
                //var result = await socket.GroupGetInviteInfo("EzZfmQJDoyY7VPklVxVV9l");
            }
        }

        private static void Auth_Update(object? sender, AuthenticationCreds e)
        {
            lock (locker)
            {
                var credsFile = Path.Join(socket.SocketConfig.CacheRoot, $"creds.json");
                var json = AuthenticationCreds.Serialize(e);
                File.WriteAllText(credsFile, json);
            }
        }









    }
}
