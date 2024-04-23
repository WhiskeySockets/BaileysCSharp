using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Events.Stores
{
    public interface IUpsertable<T>
    {
        public event EventHandler<T> Upsert;
    }

    public interface IUpdateable<T>
    {
        public event EventHandler<T> Update;
    }

    public interface IDeleteable<T>
    {
        public  event EventHandler<T> Delete;
    }

    public abstract class DataEventStore<T> : IEventStore<T>
    {
        Dictionary<EmitType, List<T>> internalData;
        public bool IsBufferable { get; }
        public BaseSocket Sender { get; }

        public DataEventStore(bool isBufferable)
        {
            IsBufferable = isBufferable;
            internalData = new Dictionary<EmitType, List<T>>();
            internalData[EmitType.Set] = new List<T>();
            internalData[EmitType.Upsert] = new List<T>();
            internalData[EmitType.Update] = new List<T>();
            internalData[EmitType.Delete] = new List<T>();
            internalData[EmitType.Reaction] = new List<T>();
        }

        public void Flush()
        {
            foreach (var item in internalData)
            {
                if (item.Value.Count > 0)
                {
                    Execute(item.Key, item.Value.ToArray());
                    item.Value.Clear();
                }
            }
        }

        public void Emit(EmitType action, params T[] data)
        {
            if (IsBufferable)
            {
                internalData[action].AddRange(data);
            }
            else
            {
                Execute(action, data);
            }
        }

        public abstract void Execute(EmitType value, T[] args);
    }
}
