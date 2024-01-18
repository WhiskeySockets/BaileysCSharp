using System.Diagnostics;
using System.Text;
using WebSocketSharp;
using WhatsSocket.Core;
using WhatsSocket.Core.Credentials;
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Sockets;
using Logger = WhatsSocket.Core.Helper.Logger;

namespace WhatsSocket
{
    //Test data from nodejs to check if it is maybe headers. but it is not
    public class Behaviour : WebSocketSharp.Server.WebSocketBehavior
    {
        WebSocketClient client;
        public Behaviour()
        {
            client = new WebSocketClient();
            client.MessageRecieved += Client_MessageRecieved;
            Task.Run(client.Connect);
        }
        private void Client_MessageRecieved(AbstractSocketClient sender, byte[] frame)
        {
            this.Send(frame);
        }
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);
        }
        protected override void OnOpen()
        {
            base.OnOpen();
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            while (!client.IsConnected)
            {
                Thread.Sleep(100);
            }
            client.Send(e.RawData);
        }
    }

    internal class Program
    {
        static WhatsAppSocket socket;
        static void Main(string[] args)
        {

            //StartServer();

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