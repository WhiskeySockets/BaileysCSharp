using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace WhatsSocket.Core.Sockets
{
    public class WebSocketClient : AbstractSocketClient
    {
        WebSocket socket;
        public WebSocketClient()
        {
            socket = new WebSocket("wss://web.whatsapp.com/ws/chat");
            socket.Origin = "https://web.whatsapp.com";
            socket.Compression = CompressionMethod.Deflate;//?
            socket.WaitTime = TimeSpan.FromSeconds(20);
            socket.OnOpen += Socket_OnOpen;
            socket.OnClose += Socket_OnClose;
            socket.OnMessage += Socket_OnMessage;
            socket.OnError += Socket_OnError;
            //socket = new ClientWebSocket();
            //socket.Options.SetRequestHeader("Origin", "https://web.whatsapp.com");
            //socket.Options.SetRequestHeader("Host", "web.whatsapp.com");
            ////socket.Options.SetRequestHeader("Sec-WebSocket-Version", "13");
            ////socket.Options.SetRequestHeader("Sec-WebSocket-Key", "2ioBLj3YJ0hEEBKf8E5X1A==");
            //socket.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
        }

        private void Socket_OnError(object? sender, WebSocketSharp.ErrorEventArgs e)
        {
            OnError(e.Message);
        }

        private void Socket_OnMessage(object? sender, MessageEventArgs e)
        {
            if (e.IsBinary)
            {
                EmitReceivedData(e.RawData);
                return;
            }
            if (e.IsPing)
            {
                OnPing();
            }
        }

        private void Socket_OnClose(object? sender, CloseEventArgs e)
        {
            IsConnected = false;
            OnDisconnected();
        }

        private void Socket_OnOpen(object? sender, EventArgs e)
        {
            IsConnected=true;
            OnOpened();
        }

        public override void Connect()
        {
            IsConnected = false;
            socket.Connect();
        }


        //private async void StartReceiving(object? obj)
        //{
        //    while (socket.State == WebSocketState.Open)
        //    {
        //        try
        //        {
        //            var byteBuffer = new byte[1024];
        //            var result = await socket.ReceiveAsync(byteBuffer, CancellationToken.None);
        //            var encrypted = byteBuffer.Take(result.Count).ToArray();
        //            EmitReceivedData(encrypted);
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //    OnDisconnected();
        //}

        public override async void Disconnect()
        {
            socket.Close();
            //if (socket.State == WebSocketState.Open)
            //{
            //    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bey", CancellationToken.None);
            //    OnDisconnected();
            //}
        }

        public override async void Send(byte[] data)
        {
            socket.Send(data);
            //if (socket.State != WebSocketState.Open)
            //{
            //    throw new Exception("Cannot send data if not connected");
            //}
            //await socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
