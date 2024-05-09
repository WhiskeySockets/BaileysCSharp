using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Utils
{
    public class MimeTypeUtils
    {
        private static readonly IDictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly IDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        static void Init()
        {
            var assembly = typeof(MimeTypeUtils).Assembly;
            var resources = assembly.GetManifestResourceNames();
            Stream? resource = assembly?.GetManifestResourceStream("BaileysCSharp.Resources.mimeTypes.json");
            if (resource != null)
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(resource);

                var grouped = result.GroupBy(x => x.Value).Where(x => x.Count() > 1).ToList();

                if (result != null)
                {
                    foreach (var item in result)
                    {
                        MimeTypes.Add(item);
                        Extensions[item.Value] = item.Key;
                    }
                }
                Extensions["audio/ogg"] = "ogg";
            }
        }
        static MimeTypeUtils()
        {
            Init();
        }

        public static string GetMimeType(FileInfo file)
        {
            return GetMimeType(file.Extension);
        }

        public static string GetMimeType(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "application/octet-stream";
            }

            var parsedExtension = Path.GetExtension(extension);
            if (!string.IsNullOrEmpty(parsedExtension))
            {
                extension = parsedExtension;
            }

            extension = extension.Replace('.', '\0');
            return MimeTypes.TryGetValue(extension, out var mime) ? mime : "application/octet-stream";
        }
        public static string GetExtension(string miometype)
        {
            if (string.IsNullOrWhiteSpace(miometype))
            {
                return "";
            }
            miometype = miometype.Split(';')[0].Trim();
            return Extensions.TryGetValue(miometype, out var mime) ? $"{mime}" : "";
        }
    }
}
