using System.Diagnostics;
using System.Text;
using WhatsSocket.Core;
using WhatsSocket.Core.Credentials;
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.TestModels;
using Logger = WhatsSocket.Core.Helper.Logger;

namespace WhatsSocket
{

    internal class Program
    {
        static WhatsAppSocket socket;
        static void Main(string[] args)
        {

            //StartServer();

            TestDecodeFrame();
            TestHKDIF();
            TestEncoder();


            //This creds file comes from the nodejs sample    
            var credsFile = Path.Join(Directory.GetCurrentDirectory(), "baileys_auth_info.json");


            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();



            socket = new WhatsAppSocket(authentication, new Logger());


            socket.MakeSocket();

            Console.ReadLine();
        }

        private static void TestDecodeFrame()
        {
            var decrypted = Convert.FromBase64String("APgKHgb6AAMEPgj7hSYngHkPGfwCbWT4AfgC/AtwYWlyLWRldmljZfgG+AL8A3JlZvxSMkBaMklsOWdJaDhFUWxKTDJlY3FjZjVjNG1YSE5ZZlMrdWIwYTlpV0ZIay9lSHg1TkhrQ2dDZk50c0hIUmt6S3VqeUdoSmc5WUswOTdBWkE9PfgC/ANyZWb8UjJAT0VXdENLNElJdUZrdytvUGhkNHdmRWgxQ21CWlZCMEZZVGcxZkdadGpXWmlBS29EWXk1NXBuZ0FPb2QvMTRESDJRSzdraE54cGZ6aHpBPT34AvwDcmVm/FIyQHF6a3RmdDRBTW0vY1Q2ZUtyUllhNWZmK2lEVnl0UmdHSVRPSHQzOG53RmtIdEpLV0crTlgzT0wwYjFBUGdwNFE5QkVZclJlZ0hnQiszUT09+AL8A3JlZvxSMkBsVEdwMExZQ3UzazMvL1lqRkVZMmVjKytHaG1YcDNHN3c4aExEMVR5ZmdKMTZrWGM2bGlkd0FlNjczb0dhaDFYV2RFV1VQZWZWRWNGMXc9PfgC/ANyZWb8UjJAaUx1WTBEdDB3QU10QWJLNEhURG85L0NuUHBUQWQ0eHh2MThqWWxpOXFadlFsSTd2NjdUMlUwbjhPeHU0NGFCbXNuUGJJWnZrbmY4WWNnPT34AvwDcmVm/FIyQFFCN1gycm1ocnNvdHNZb0Z4emRVeHhJQmFjazhZajYxQzM0RHlUQktUWXNlSU5LeW1ockdYUWRkaWlZVCtSN3JvRzV3eHgzR3phS0l0UT09");
            var node= BufferDecoder.DecodeBinaryNode(decrypted);
            Debug.Assert(node.attrs["id"] == "262780790");
        }

        private static void StartServer()
        {
            var server = new WebSocketSharp.Server.WebSocketServer(533);
            server.AddWebSocketService<Behaviour>("/ws/chat");
            server.Start();

        }

        private static void TestEncoder()
        {
            //Test From First Send Node
            //Result should be if id is 
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
            var base64 = Convert.ToBase64String(data);
            Debug.Assert(base64 == "APgHHg76AAMEFAj/BRdjVmFn");



            //Check EncodeFrame
            var noise = new NoiseHandler(new KeyPair() { Public = new byte[0], Private = new byte[0] }, new Logger());
            noise.EncKey = Convert.FromBase64String("IQ0axqM7VdppHVYjjgv+f1hv0ioMHb8zWPJqRBTCB+I=");
            noise.IsFinished = true;
            noise.SetIntro = true;
            noise.Hash = new byte[0];
            var resultToSend = noise.EncodeFrame(data);
            var resultb64 = Convert.ToBase64String(resultToSend);
            Debug.Assert(resultb64 == "AAAiqGhn9eons9qb858cYl4gHUlphcTYWJdLhZ5DCNIRHB+z/Q==");


        }

        private static void TestHKDIF()
        {
            /* RESULT FROM NODE PROJECT ON HKDIF
            HKDIF 1
{
  kdf: 'frvaFLM4ZMjur+3gOkWkOJh3Phpb1Q2voTS2tVkDCzU=',
  salt: 'Tm9pc2VfWFhfMjU1MTlfQUVTR0NNX1NIQTI1NgAAAAA=',
  key: 'nc0uKa/NKY545AdJxcom2iBZy6xAUJqt+UgJDAn/KxTiM0+nIw7VqbX5Y4dvcq9eA3o6TjdZRtFxUY0gBTSBAw==',
  s1: 'nc0uKa/NKY545AdJxcom2iBZy6xAUJqt+UgJDAn/KxQ=',
  s2: '4jNPpyMO1am1+WOHb3KvXgN6Ok43WUbRcVGNIAU0gQM='
}
            */
            var hkdf = EncryptionHelper.HKDF(Convert.FromBase64String("frvaFLM4ZMjur+3gOkWkOJh3Phpb1Q2voTS2tVkDCzU="), 64, Convert.FromBase64String("Tm9pc2VfWFhfMjU1MTlfQUVTR0NNX1NIQTI1NgAAAAA="), Encoding.UTF8.GetBytes(""));
            var b64 = Convert.ToBase64String(hkdf);

            var split1 = Convert.ToBase64String(hkdf.Take(32).ToArray());
            var split2 = Convert.ToBase64String(hkdf.Skip(32).ToArray());
            Debug.Assert(split1 == "nc0uKa/NKY545AdJxcom2iBZy6xAUJqt+UgJDAn/KxQ=");
            Debug.Assert(split2 == "4jNPpyMO1am1+WOHb3KvXgN6Ok43WUbRcVGNIAU0gQM=");
        }

        private static void Socket_OpenEvent(object? sender, EventArgs e)
        {
            socket.ValidateConnection();
        }

        private async static Task Run()
        {

            Console.ReadLine();
        }

        //private static async void ReadThread(object? obj)
        //{
        //    byte[] buffer = new byte[1024];
        //    ArraySegment<byte> bytes = new ArraySegment<byte>(buffer);
        //    while (webClient.State == WebSocketState.Open)
        //    {
        //        var result = await webClient.ReceiveAsync(bytes, CancellationToken.None);


        //    }
        //}
    }


}