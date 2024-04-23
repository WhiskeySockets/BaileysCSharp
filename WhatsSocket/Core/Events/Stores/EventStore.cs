using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Events
{

    public class EventStore<T> : IEventStore
    {
        public event EventEmitterHandler<T[]> Multi;
        public event EventEmitterHandler<T> Single;
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
                    if (item[0] is IEventManage)
                    {
                        foreach (var newEntry in item)
                        {
                            if (newEntry is IEventManage newevent)
                            {
                                IEventManage canlink = null;
                                foreach (IEventManage? existing in Items)
                                {
                                    if (existing != null)
                                    {
                                        if (existing.CanMerge(newevent))
                                        {
                                            canlink = existing;
                                        }
                                    }
                                }
                                if (canlink == null)
                                {
                                    Items.Add(newEntry);
                                }
                                else
                                {
                                    canlink.Merge(newevent);
                                }
                            }
                        }
                    }
                    else
                    {
                        Items.AddRange(item);
                    }

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
                if (Single!= null)
                {
                    foreach (var item in args)
                    {
                        Single.Invoke(item);
                    }
                }

                if (Multi != null)
                {
                    Debug.WriteLine($"Flushed {args.Length} of type {typeof(T).Name}");
                    Multi.Invoke(args.ToArray());
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// confirm if this works
        /// </summary>
        void IEventStore.Flush()
        {
            Flush();
        }
    }
}
