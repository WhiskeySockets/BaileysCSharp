using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsSocket.Core.Sockets
{
    public abstract class AbstractSocketClient
    {
        public delegate void OnReceiveArgs(AbstractSocketClient sender, byte[] frame);
        public event OnReceiveArgs MessageRecieved;

        public event EventHandler Opened;
        //public event EventHandler Ping;
        public event EventHandler Disconnected;
        public event EventHandler ConnectFailed;
        public event EventHandler<string> Error;

        public bool IsConnected { get; protected set; }


        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(byte[] data);

        protected void EmitReceivedData(byte[] data)
        {
            MessageRecieved?.Invoke(this, data);
        }

        public void OnOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        public void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
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
