namespace BaileysCSharp.Core.Events
{
    public interface IEventStore
    {
        void Flush();
    }
    public interface IEventStore<T> : IEventStore
    {
        void Emit(EmitType action, params T[] data);
    }
}
