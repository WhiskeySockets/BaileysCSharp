using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.NoSQL
{
    public class KeyStore : IDisposable
    {
        LiteDatabase? context;

        public KeyStore(string path)
        {
            context = new LiteDatabase($"{path}\\config.db");
        }

        public void Dispose()
        {
            context?.Dispose();
            context = null;
        }

        public void Set<T>(string id, T? value)
        {
            if (value != null)
            {
                var bson = context?.GetCollection<T>().Insert(value);
            }
            else
            {
                context.GetCollection<T>().Delete(id);
            }
        }
        public void Set<T>(T[]? values)
        {
            context?.GetCollection<T>().InsertBulk(values);
        }

        internal object All<T>()
        {
            throw new NotImplementedException();
        }

        internal T Get<T>(string id)
        {
            var collection = context?.GetCollection<T>();
            var result = (T)collection.FindById(id);
            return result;
        }

        internal T[] Range<T>(List<string> ids)
        {
            List<T> result = new List<T>(); 
            var collection = context?.GetCollection<T>();
            foreach (var item in ids)
            {
                var existing = collection.FindById(item);
                if (existing != null)
                {
                    result.Add(existing);
                }
            }
            return result.ToArray();
        }
    }

    public class PreKeyPair
    {
        public PreKeyPair(string id, KeyPair? key)
        {
            Id = id;
            Key = key;
        }

        [BsonId]
        public string Id { get; }
        public KeyPair Key { get; set; }
    }
}
