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
using System.Text.Json;
using System.Text;
using BaileysCSharp.Core.Logging;
using BaileysCSharp.Core.WABinary;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static List<WebMessageInfo> messages = new List<WebMessageInfo>();
        static WASocket socket;
        public static object locker = new object();

        static void Main(string[] args)
        {


            var node = BufferReader.DecodeDecompressedBinaryNode(Convert.FromBase64String("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"));
            byte[] routingInfo = [8, 11, 8, 9];
            var header = new byte[7];



            var node2 = new BinaryNode()
            {
                tag = "message",
                attrs =
                {
                    { "to", "120363304341918870@newsletter" },
                    { "id", "3EB09D6BB0229376644422" },
                    { "media_id", "wa_channel_image/flat/AA4C870CB809F65F0B99608B6EB89C8A" },
                    { "type", "media" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "plaintext",
                        attrs =
                        {
                            {"mediatype","image" }
                        },
                        content = Convert.FromBase64String("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")
                    }
                }
            };

            var buffer = BufferWriter.EncodeBinaryNode(node2);


            var config = new SocketConfig()
            {
                SessionName = "27665458845745067",
            };

            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();

            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);

            config.Logger.Level = LogLevel.Raw;
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
            Console.WriteLine(JsonSerializer.Serialize(e));
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

                    if (msg.Message.ImageMessage != null)
                    {
                        var result = await socket.DownloadMediaMessage(msg);
                    }

                    if (msg.Message.DocumentMessage != null)
                    {
                        var result = await socket.DownloadMediaMessage(msg);
                        File.WriteAllBytes(result.FileName, result.Data);
                    }

                    if (msg.Message.AudioMessage != null)
                    {
                        var result = await socket.DownloadMediaMessage(msg);
                        File.WriteAllBytes($"audio.{MimeTypeUtils.GetExtension(result.MimeType)}", result.Data);
                    }
                    if (msg.Message.VideoMessage != null)
                    {
                        var result = await socket.DownloadMediaMessage(msg);
                        File.WriteAllBytes($"video.{MimeTypeUtils.GetExtension(result.MimeType)}", result.Data);
                    }
                    if (msg.Message.StickerMessage != null)
                    {
                        var result = await socket.DownloadMediaMessage(msg);
                        File.WriteAllBytes($"sticker.{MimeTypeUtils.GetExtension(result.MimeType)}", result.Data);
                    }

                    if (msg.Message.ExtendedTextMessage == null)
                        continue;

                    if (msg.Key.FromMe == false && msg.Message.ExtendedTextMessage != null && msg.Message.ExtendedTextMessage.Text == "runtests")
                    {
                        var jid = JidUtils.JidDecode(msg.Key.Id);
                        // send a simple text!
                        var standard = await socket.SendMessage(msg.Key.RemoteJid, new TextMessageContent()
                        {
                            Text = "Hi there from C#",
                        });

                        ////send a reply messagge
                        //var quoted = await socket.SendMessage(msg.Key.RemoteJid,
                        //    new TextMessageContent() { Text = "Hi this is a C# reply" },
                        //    new MessageGenerationOptionsFromContent()
                        //    {
                        //        Quoted = msg
                        //    });
                        //
                        //
                        //// send a mentions message
                        //var mentioned = await socket.SendMessage(msg.Key.RemoteJid, new TextMessageContent()
                        //{
                        //    Text = $"Hi @{jid.User} from C# with mention",
                        //    Mentions = [msg.Key.RemoteJid]
                        //});
                        //
                        //// send a contact!
                        //var contact = await socket.SendMessage(msg.Key.RemoteJid, new ContactMessageContent()
                        //{
                        //    Contact = new ContactShareModel()
                        //    {
                        //        ContactNumber = jid.User,
                        //        FullName = $"{msg.PushName}",
                        //        Organization = ""
                        //    }
                        //});
                        //
                        //// send a location! //48.858221124792756, 2.294466243303683
                        //var location = await socket.SendMessage(msg.Key.RemoteJid, new LocationMessageContent()
                        //{
                        //    Location = new Message.Types.LocationMessage()
                        //    {
                        //        DegreesLongitude = 48.858221124792756,
                        //        DegreesLatitude = 2.294466243303683,
                        //    }
                        //});
                        //
                        ////react
                        //var react = await socket.SendMessage(msg.Key.RemoteJid, new ReactMessageContent()
                        //{
                        //    Key = msg.Key,
                        //    ReactText = "💖"
                        //});
                        //
                        //// Sending image
                        //var imageMessage = await socket.SendMessage(msg.Key.RemoteJid, new ImageMessageContent()
                        //{
                        //    Image = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\cat.jpeg", FileMode.Open),
                        //    Caption = "Cat.jpeg"
                        //});
                        //
                        //// send an audio file
                        //var audioMessage = await socket.SendMessage(msg.Key.RemoteJid, new AudioMessageContent()
                        //{
                        //    Audio = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\sonata.mp3", FileMode.Open),
                        //});
                        //
                        //// send an audio file
                        //var videoMessage = await socket.SendMessage(msg.Key.RemoteJid, new VideoMessageContent()
                        //{
                        //    Video = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\ma_gif.mp4", FileMode.Open),
                        //    GifPlayback = true
                        //});
                        // 
                        //// send a document file
                        //var documentMessage = await socket.SendMessage(msg.Key.RemoteJid, new DocumentMessageContent()
                        //{
                        //    Document = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\file.pdf", FileMode.Open),
                        //    Mimetype = "application/pdf",
                        //    FileName = "proposal.pdf",
                        //});



                        //var group = await socket.GroupCreate("Test", [chatId]);
                        //await socket.GroupUpdateSubject(groupId, "Subject Nice");
                        //await socket.GroupUpdateDescription(groupId, "Description Nice");


                        // send a simple text!
                        //var standard = await socket.SendMessage(groupId, new TextMessageContent()
                        //{
                        //    Text = "Hi there from C#"
                        //});


                        //var groupId = "@g.us";
                        //var chatId = "@s.whatsapp.net";
                        //var chatId2 = "@s.whatsapp.net";

                        //await socket.GroupSettingUpdate(groupId, GroupSetting.Not_Announcement);

                        //await socket.GroupMemberAddMode(groupId, MemberAddMode.All_Member_Add); 

                        //await socket.GroupParticipantsUpdate(groupId, [chatId2], ParticipantAction.Promote);
                        //await socket.GroupParticipantsUpdate(groupId, [chatId2], ParticipantAction.Demote);

                        //var result = await socket.GroupInviteCode(groupId);
                        //var result = await socket.GroupGetInviteInfo("EzZfmQJDoyY7VPklVxVV9l");
                    }


                    messages.Add(msg);
                }
            }
        }

        private static async void Connection_Update(object? sender, ConnectionState e)
        {
            var connection = e;
            Debug.WriteLine(JsonSerializer.Serialize(connection));
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
                //var result = await socket.QueryRecommendedNewsletters();
                //var letter = result.Result[0];
                //await socket.NewsletterFollow(letter.Id);
                //await socket.NewsletterMute(letter.Id);
                //await socket.NewsletterUnMute(letter.Id);
                //await socket.NewsletterUnFollow(letter.Id);




                //await socket.AcceptTOSNotice();
                //var nl = await socket.NewsletterCreate("Test Newsletter");
                //await socket.NewsletterUpdateName(nl.Id, "Hello Ignus");
                //await socket.NewsletterUpdateDescription(nl.Id, "Newsletter Description");
                //var admin = await socket.NewsletterAdminCount(nl.Id);

                //var info = await socket.NewsletterMetadata("120363184364170818@newsletter", BaileysCSharp.Core.Models.Newsletters.NewsletterMetaDataType.JID);



                //var snd = await socket.SendNewsletterMessage("120363285541953068@newsletter", new NewsletterTextMessage()
                //{
                //    Text = "Hello Channel"
                //});

                //await socket.NewsletterDelete(nl.Id);
                //var imageMessage = await socket.SendMessage(nl.Id, new ImageMessageContent()
                //{
                //    Image = File.Open($"{Directory.GetCurrentDirectory()}\\Media\\icon.png", FileMode.Open),
                //    Caption = "Cat.jpeg"
                //});

                //Thread.Sleep(10000);
                //await socket.NewsletterDelete(nl.Id);

                //var standard = await socket.SendMessage("27797798179@s.whatsapp.net", new TextMessageContent()
                //{
                //    Text = "Hi there from C#",
                //});

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
