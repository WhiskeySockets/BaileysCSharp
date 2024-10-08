using BaileysCSharp.Core.Converters;
using BaileysCSharp.Core.Helper;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace BaileysCSharp.Core.NoSQL
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
                if (memory.TryGetValue($"{attributes.Prefix}-{id}", out var value))
                {
                    return (T)value;
                }

                var path = System.IO.Path.Combine(Path, attributes.Prefix);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var file = $"{path}\\{attributes.Prefix}-{id.Replace("/", "__").Replace("::", "__")}.json";

                if (File.Exists(file))
                {
                    var mv = file;
                    file = $"{path}\\{id.Replace("/", "__").Replace("::", "__")}.json";
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                    File.Move(mv, file);
                }

                file = $"{path}\\{id.Replace("/", "__").Replace("::", "__")}.json";
                if (File.Exists(file))
                {
                    var data = File.ReadAllText(file) ?? "";
                    data = data.Replace("pubKey", "public");
                    data = data.Replace("privKey", "private");
                    try
                    {
                        return JsonSerializer.Deserialize<T>(data);
                    }
                    catch (Exception)
                    {
                        return JsonSerializer.Deserialize<T>(data, new JsonSerializerOptions()
                        {
                            Converters =
                            {
                                new BufferConverter()
                            }
                        });
                    }
                }
                return default(T);
            }
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

        Dictionary<string, object> memory = new Dictionary<string, object>();

        public override void Set<T>(string id, T? value) where T : default
        {
            lock (locker)
            {
                var attributes = typeof(T).GetCustomAttribute<FolderPrefix>();
                var path = System.IO.Path.Combine(Path, attributes.Prefix);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                var file = $"{path}\\{id.Replace("/", "__").Replace("::", "__")}.json";

                if (value != null)
                {
                    memory[$"{attributes.Prefix}-{id}"] = value;
                    File.WriteAllText(file, JsonSerializer.Serialize(value, JsonHelper.Options));
                }
                else if (File.Exists(file))
                {
                    memory.Remove($"{attributes.Prefix}-{id}");
                }
            }
        }
    }
}
