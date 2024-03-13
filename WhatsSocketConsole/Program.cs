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
using WhatsSocket.Core.Curve;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Extensions;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static BaseSocket socket;
        static void Main(string[] args)
        {
            Tests.RunTests();



            //This creds file comes from the nodejs sample    





            var config = new SocketConfig()
            {
                ID = "27665245067",
            };


            var credsFile = Path.Join(config.CacheRoot, "creds.json");
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

            socket = new BaseSocket(config);


            socket.EV.OnCredsChange += Socket_OnCredentialsChangeArgs;
            socket.EV.OnDisconnect += EV_OnDisconnect;
            socket.EV.OnQR += EV_OnQR;
            socket.EV.OnMessageUpserted += EV_OnMessageUpserted;


            socket.MakeSocket();

            Console.ReadLine();
        }

        private static void EV_OnMessageUpserted(BaseSocket sender, (WebMessageInfo[] newMessages, string type) args)
        {
            //Notify is nuut
            //Append is oud (gewoonlik as service af was)
            foreach (var item in args.newMessages)
            {
                var json = item.ToJson();
                Console.WriteLine(json);
            }
        }

        private static void EV_OnQR(BaseSocket sender, QRData args)
        {

            QRCodeGenerator QrGenerator = new QRCodeGenerator();
            QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(args.Data, QRCodeGenerator.ECCLevel.L);
            AsciiQRCode qrCode = new AsciiQRCode(QrCodeInfo);
            var data = qrCode.GetGraphic(1);

            Console.WriteLine(data);
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



        private static void Socket_OnCredentialsChangeArgs(BaseSocket sender, AuthenticationCreds authenticationCreds)
        {

            var credsFile = Path.Join(sender.SocketConfig.CacheRoot, $"creds.json");
            var json = AuthenticationCreds.Serialize(authenticationCreds);
            File.WriteAllText(credsFile, json);
        }


    }
}
