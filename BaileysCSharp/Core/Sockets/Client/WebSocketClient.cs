using Org.BouncyCastle.Utilities;
using System.Diagnostics;
using System.Net.WebSockets;
namespace BaileysCSharp.Core.Sockets.Client
{
    public class WebSocketClient : AbstractSocketClient
    {
        ClientWebSocket WebSocket;

        public override void Dispose()
        {
            base.Dispose();
            WebSocket.Abort();
            WebSocket?.Dispose();
        }
        public WebSocketClient(BaseSocket wasocket) : base(wasocket)
        {
        }

        public override void MakeSocket()
        {
            WebSocket?.Dispose();
            WebSocket = new ClientWebSocket();
            WebSocket.Options.SetRequestHeader("Origin", "https://web.whatsapp.com");
            WebSocket.Options.SetRequestHeader("Host", "web.whatsapp.com");
            WebSocket.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
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
                await WebSocket.ConnectAsync(new Uri("wss://web.whatsapp.com/ws/chat"), CancellationToken.None);

                if (WebSocket.State == WebSocketState.Open)
                {
                    IsConnected = true;
                    OnOpened();
                    while (WebSocket.State == WebSocketState.Open)
                    {
                        var sizeBuffer = await ReadBytes(3);
                        // the binary protocol uses its own framing mechanism
                        // on top of the WS frames
                        // so we get this data and separate out the frames
                        var messageSize = sizeBuffer[0] >> 16 | BitConverter.ToUInt16(sizeBuffer.Skip(1).Reverse().ToArray());

                        //Read the frame based on the size
                        var frame = await ReadBytes(messageSize);
                        EmitReceivedData(frame);
                    }
                }
            }
            catch (Exception ex)
            {
                OnDisconnected(Events.DisconnectReason.ConnectionLost);
            }
        }


        private async Task<byte[]> ReadBytes(int size)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var received = 0;
                while (received < size)
                {
                    var byteBuffer = new byte[size - received];
                    var result = await WebSocket.ReceiveAsync(byteBuffer, CancellationToken.None);
                    received += result.Count;
                    stream.Write(byteBuffer, 0, result.Count);
                }
                return stream.ToArray();
            }
        }

        //private async void ReceivingHandler(object? state)
        //{
        //    try
        //    {
        //        await socket.ConnectAsync(new Uri("wss://web.whatsapp.com/ws/chat"), CancellationToken.None);
        //        if (socket.State == WebSocketState.Open)
        //        {
        //            IsConnected = true;
        //            OnOpened();
        //            while (socket.State == WebSocketState.Open)
        //            {
        //                var byteBuffer = new byte[1024];
        //                var result = await socket.ReceiveAsync(byteBuffer, CancellationToken.None);
        //                var encrypted = byteBuffer.Take(result.Count).ToArray();
        //                EmitReceivedData(encrypted);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OnDisconnected(Events.DisconnectReason.ConnectionLost);
        //    }
        //}



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
            if (WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bey", CancellationToken.None);
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

        public override async Task Send(byte[] data)
        {
            if (WebSocket.State != WebSocketState.Open)
            {
                Socket.Logger?.Error(string.Format("[Error on Send Menth]-[IsConnected={0}]-[WebSocket.State={1}] ", IsConnected, WebSocket.State));
                if (IsConnected)
                    OnConnectFailed();
                throw new Exception("Cannot send data if not connected");
            }
            await WebSocket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
