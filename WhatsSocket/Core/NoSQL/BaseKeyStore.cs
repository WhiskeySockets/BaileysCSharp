namespace BaileysCSharp.Core.NoSQL
{
    public abstract class BaseKeyStore : IDisposable
    {
        public string Path { get; }

        public virtual void Dispose()
        {

        }

        public abstract T Get<T>(string id);
        public abstract T[] Range<T>(List<string> ids);
        public abstract void Set<T>(string id, T? value);
        public abstract Dictionary<string, T> Get<T>(List<string> ids);
        public Dictionary<string, T> Get<T>(string[] ids)
        {
            return Get<T>(ids.ToList());
        }
    }

}
