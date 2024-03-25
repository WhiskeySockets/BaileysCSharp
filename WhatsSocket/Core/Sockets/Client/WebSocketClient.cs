using System.Net.WebSockets;
namespace WhatsSocket.Core.Sockets.Client
{
    public class WebSocketClient : AbstractSocketClient
    {
        ClientWebSocket socket;
        public WebSocketClient()
        {
            socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Origin", "https://web.whatsapp.com");
            socket.Options.SetRequestHeader("Host", "web.whatsapp.com");
            socket.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
        }
        public override void Connect()
        {
            IsConnected = false;
            ThreadPool.QueueUserWorkItem(ReceivingHandler);
        }

        private async void ReceivingHandler(object? state)
        {
            try
            {
                await socket.ConnectAsync(new Uri("wss://web.whatsapp.com/ws/chat"), CancellationToken.None);
                if (socket.State == WebSocketState.Open)
                {
                    IsConnected = true;
                    OnOpened();
                    while (socket.State == WebSocketState.Open)
                    {
                        var byteBuffer = new byte[1024];
                        var result = await socket.ReceiveAsync(byteBuffer, CancellationToken.None);
                        var encrypted = byteBuffer.Take(result.Count).ToArray();
                        EmitReceivedData(encrypted);
                    }
                }
            }
            catch (Exception ex)
            {
                OnDisconnected(Events.DisconnectReason.ConnectionLost);
            }
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
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bey", CancellationToken.None);
                }
                catch (OperationCanceledException ex)
                {

                }
                catch (WebSocketException ex)
                {
                }
                OnDisconnected(Events.DisconnectReason.ConnectionClosed);
            }
        }

        public override async void Send(byte[] data)
        {
            if (socket.State != WebSocketState.Open)
            {
                throw new Exception("Cannot send data if not connected");
            }
            await socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
