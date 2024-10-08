using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Events
{
    public class NodeCache : BaseKeyStore
    {
        public Dictionary<string, object> Cache = new Dictionary<string, object>();

        public NodeCache()
        {
        }

        public override T Get<T>(string id)
        {
            if (Cache.TryGetValue(id, out var value))
            {
                if (value is T)
                {
                    return (T)value;
                }
            }
            return default;
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
}
