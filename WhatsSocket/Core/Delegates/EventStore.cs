using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsSocket.Core.Delegates
{
    internal interface IEventStore
    {
        internal void Flush();

    }

    public class EventStore<T> : IEventStore
    {
        public event EventEmitterHandler<T[]> Emit;
        private List<T> Items { get; set; }
        public bool IsBufferable { get; }
        public BaseSocket Sender { get; }

        private EventStore(bool isBufferable)
        {
            IsBufferable = isBufferable;
            Items = new List<T>();
        }

        public EventStore(BaseSocket sender, bool isBufferable) : this(isBufferable)
        {
            Sender = sender;
        }

        internal void Append(T[] item)
        {
            if (item.Length > 0)
            {
                if (IsBufferable)
                {
                    //
                    Items.AddRange(item);
                }
                else
                {
                    Execute(item);
                }
            }
        }

        internal void Flush()
        {
            if (Items.Count > 0)
            {
                Execute(Items.ToArray());
                Items.Clear();
            }
        }

        private bool Execute(T[] args)
        {
            if (args.Length > 0)
            {
                Debug.WriteLine($"Flushed {Items.Count} of type {typeof(T).Name}");
                Emit?.Invoke(Sender, args.ToArray());
                return true;
            }
            return false;
        }


        /// <summary>
        /// confirm if this works
        /// </summary>
        void IEventStore.Flush()
        {
            this.Flush();
        }
    }
}
