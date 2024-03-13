using Google.Protobuf;
using Newtonsoft.Json;
using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Extensions
{
    public class IgnoreFalseBool : JsonConverter<bool>
    {
        public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
        {
            if (value)
            {
                writer.WriteValue("true");
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    public static class Extensions
    {
        public static string ToJson(this IMessage proto)
        {// Convert the message to JSON with indentation
            JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation());
            string jsonString = formatter.Format(proto);
            return jsonString;
        }
    }
}
