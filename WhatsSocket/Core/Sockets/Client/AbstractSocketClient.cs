using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Sockets.Client
{
    public abstract class AbstractSocketClient
    {
        Dictionary<string, NodeEventStore> Events;
        protected AbstractSocketClient(BaseSocket socket)
        {
            Socket = socket;
            Events = new Dictionary<string, NodeEventStore>();
        }

        public event MessageArgs MessageRecieved;

        public event ConnectEventArgs Opened;
        public event DisconnectEventArgs Disconnected;
        public event EventHandler ConnectFailed;
        public event EventHandler<string> Error;

        public bool IsConnected { get; protected set; }
        public BaseSocket Socket { get; }

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



        //public bool Emit(string type, BinaryNode args)
        //{
        //    if (!Events.ContainsKey(type))
        //    {
        //        Events[type] = new NodeEventStore(Socket);
        //    }
        //    var store = Events[type];
        //    var result = store.Execute(args);
        //    if (result)
        //    {
        //        Debug.Write($"{type} has been executed");
        //    }
        //    return result;
        //}

        //public NodeEventStore On(string type)
        //{
        //    if (!Events.ContainsKey(type))
        //    {
        //        Events[type] = new NodeEventStore(Socket);
        //    }
        //    var store = Events[type];
        //    return store;
        //}

        public abstract void MakeSocket();
    }
}
