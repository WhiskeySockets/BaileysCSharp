using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.NoSQL
{

    public class FileKeyStore : BaseKeyStore, IDisposable
    {
        public string Path { get; set; }
        public FileKeyStore(string path) 
        {
            Path = path;
            Directory.CreateDirectory(Path);
        }


        private static object locker = new object();


        public override T Get<T>(string id)
        {
            lock (locker)
            {
                var attributes = typeof(T).GetCustomAttribute<FolderPrefix>();
                if (attributes == null)
                {
                    Debug.WriteLine($"{typeof(T).Name} does not have FolderPrefix attribute");
                    throw new NotSupportedException($"{typeof(T).Name} does not have FolderPrefix attribute");
                }

                var path = System.IO.Path.Combine(Path, attributes.Prefix);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                var file = $"{path}\\{id.Replace("/","__")}.json";
                if (File.Exists(file))
                {
                    var data = File.ReadAllText(file) ?? "";
                    return JsonConvert.DeserializeObject<T>(data);
                }
                return default(T);
            }

            //var collection = context?.GetCollection<T>();
            //var result = (T)collection.FindById(id);
            //return result;
        }

        public override Dictionary<string, T> Get<T>(List<string> ids)
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            foreach (var id in ids)
            {
                result[id] = Get<T>(id);
            }
            return result;
        }

        public override T[] Range<T>(List<string> ids)
        {
            List<T> result = new List<T>();
            foreach (string id in ids)
            {
                var entry = Get<T>(id);
                if (entry != null)
                {
                    result.Add(entry);
                }
            }
            return result.ToArray();
        }

        public override void Set<T>(string id, T? value) where T : default
        {
            lock (locker)
            {
                var attributes = typeof(T).GetCustomAttribute<FolderPrefix>();
                var path = System.IO.Path.Combine(Path, attributes.Prefix);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                var file = $"{path}\\{id.Replace("/", "__")}.json";

                if (value != null)
                {
                    File.WriteAllText(file, JsonConvert.SerializeObject(value));
                }
                else if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            //Use Below
            //var collection = context?.GetCollection<T>();
            //if (value != null)
            //{
            //    if (collection.FindById(id) == null)
            //    {
            //        collection.Insert(value);
            //    }
            //    else
            //    {
            //        collection.Update(id, value);
            //    }
            //}
            //else
            //{
            //    collection.Delete(id);
            //}
        }
    }

}
