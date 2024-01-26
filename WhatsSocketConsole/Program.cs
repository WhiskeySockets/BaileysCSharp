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
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.Sessions;

namespace WhatsSocketConsole
{
    internal class Program
    {

        static BaseSocket socket;
        static void Main(string[] args)
        {
            Tests.RunTests();

            //This creds file comes from the nodejs sample    
            var credsFile = Path.Join(Directory.GetCurrentDirectory(), "test", "creds.json");


            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();



            socket = new BaseSocket("test", authentication, new Logger());



            socket.OnCredentialsChange += Socket_OnCredentialsChangeArgs;
            socket.OnDisconnected += Socket_OnDisconnected;
            socket.OnKeyStoreChange += Socket_OnStoreChange;
            socket.OnSessionStoreChange += Socket_OnSessionStoreChange;
            socket.OnQRReceived += Socket_OnQRReceived;


            socket.MakeSocket();

            Console.ReadLine();
        }

        private static void Socket_OnSessionStoreChange(SessionStore store)
        {
            var file = Path.Join(Directory.GetCurrentDirectory(), "session.json");
            var data = JsonConvert.SerializeObject(store);
            File.WriteAllText(file, data);
        }

        private static void Socket_OnQRReceived(BaseSocket sender, string qr_data)
        {

            QRCodeGenerator QrGenerator = new QRCodeGenerator();
            QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(qr_data, QRCodeGenerator.ECCLevel.L);
            AsciiQRCode qrCode = new AsciiQRCode(QrCodeInfo);
            var data = qrCode.GetGraphic(1);

            Console.WriteLine(data);
        }

        private static void Socket_OnStoreChange(KeyStore store)
        {

        }

        private static void Socket_OnDisconnected(BaseSocket sender, DisconnectReason disconnectReason)
        {
            if (disconnectReason != DisconnectReason.LoggedOut)
            {
                sender.MakeSocket();
            }
        }

        private static void Socket_OnCredentialsChangeArgs(BaseSocket sender, AuthenticationCreds authenticationCreds)
        {
            var credsFile = Path.Join(Directory.GetCurrentDirectory(), "test", "creds.json");
            var json = AuthenticationCreds.Serialize(authenticationCreds);
            File.WriteAllText(credsFile, json);
        }


        private static void Socket_OpenEvent(object? sender, EventArgs e)
        {
            socket.ValidateConnection();
        }

    }
}
