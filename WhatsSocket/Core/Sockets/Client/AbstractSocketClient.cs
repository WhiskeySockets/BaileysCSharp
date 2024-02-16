using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Events;

namespace WhatsSocket.Core.Sockets.Client
{
    public abstract class AbstractSocketClient
    {
        public event MessageArgs MessageRecieved;

        public event ConnectEventArgs Opened;
        //public event EventHandler Ping;
        public event DisconnectEventArgs Disconnected;
        public event EventHandler ConnectFailed;
        public event EventHandler<string> Error;

        public bool IsConnected { get; protected set; }


        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(byte[] data);

        protected void EmitReceivedData(byte[] data)
        {
            MessageRecieved?.Invoke(this, new DataFrame() { Buffer = data });
        }

        public void OnOpened()
        {
            Opened?.Invoke(this);
        }

        public void OnDisconnected(DisconnectReason reason)
        {
            Disconnected?.Invoke(this, reason);
        }

        public void OnError(string message)
        {
            Error?.Invoke(this, message);
        }

        public void OnConnectFailed()
        {
            ConnectFailed?.Invoke(this, EventArgs.Empty);
        }
    }
}
