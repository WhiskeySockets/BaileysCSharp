using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WhatsSocket.Core.Sockets;

namespace WhatsSocket.Core.TestModels
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
}
