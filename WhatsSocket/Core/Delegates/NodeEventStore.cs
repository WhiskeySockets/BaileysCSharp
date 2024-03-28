using System.Diagnostics;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Delegates
{

    public class NodeCache : BaseKeyStore
    {
        public Dictionary<string,object> Cache = new Dictionary<string,object>();

        public NodeCache() 
        {
        }

        public override T Get<T>(string id)
        {
            if (Cache.ContainsKey(id))
            {
                var value = Cache[id];
                if (value is T)
                {
                    return (T)value;
                }
            }
            return default(T);
        }

        public override Dictionary<string, T> Get<T>(List<string> ids)
        {
            var result = new Dictionary<string, T>();
            foreach (var id in ids)
            {
                var value = Get<T>(id);
                if (value != null)
                {
                    result[id] = value;
                }
            }
            return result;
        }

        public override T[] Range<T>(List<string> ids)
        {
            List<T> result = new List<T>();
            foreach (var id in ids)
            {
                var value = Get<T>(id);
                if (value != null)
                {
                    result.Add(value);
                }
            }
            return result.ToArray();
        }

        public override void Set<T>(string id, T? value) where T : default
        {
            Cache[id] = value;
        }
    }

    public class NodeEventStore
    {
        public event EventEmitterHandler<BinaryNode> Emit;
        public BaseSocket Sender { get; }

        private NodeEventStore()
        {
        }

        public NodeEventStore(BaseSocket sender)
        {
            Sender = sender;
        }

        public bool Execute(BinaryNode args)
        {
            if (args != null)
            {
                if (Emit != null)
                {
                    Emit.Invoke(args);
                    return true;
                }
            }
            return false;
        }

    }
}
