using LiteDB;
using System.Collections;

namespace WhatsSocket.Core.NoSQL
{
    public class Store<T> : IEnumerable<T> where T : IMayHaveID
    {
        private ILiteCollection<T> collection;
        private List<T> list;
        public Store(LiteDatabase database)
        {
            collection = database.GetCollection<T>();
            list = collection.FindAll().ToList();
        }


        public void Add(T item)
        {
            Upsert([item]);
        }


        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public bool Delete(T item)
        {
            list.Remove(item);
            return collection.Delete(item.GetID());

        }

        public void DeleteAll()
        {
            collection.DeleteAll();
            list.Clear();
        }

        public void InsertBulk(IEnumerable<T> toAdd)
        {
            InsertIfAbsent(toAdd);
        }

        public void Upsert(IEnumerable<T> toAdd)
        {
            InsertIfAbsent(toAdd);
        }

        public T[] InsertIfAbsent(IEnumerable<T> @new)
        {
            List<T> result = new List<T>();
            foreach (var item in @new)
            {
                if (list.Any(x => x.GetID() == item.GetID()))
                {
                    continue;
                }
                list.Add(item);
                collection.Insert(item);
                result.Add(item);
            }
            return result.ToArray();
        }

        internal T? FindByID(string iD)
        {
            return list.FirstOrDefault(x => x.GetID() == iD);
        }

        internal void Update(T existing)
        {
            collection.Update(existing);
        }
    }
}
