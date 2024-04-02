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
using WhatsSocket.Core.NoSQL;

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


        public static uint ToUInt32(this string? info)
        {
            info = info.Trim(' ', '\n', '\r');
            if (string.IsNullOrEmpty(info))
            {
                return 0;
            }
            return Convert.ToUInt32(info);
        }

        public static ulong ToUInt64(this string? info)
        {
            info = info.Trim(' ', '\n', '\r');
            if (string.IsNullOrEmpty(info))
            {
                return 0;
            }
            return Convert.ToUInt32(info);
        }


        public static void Update<T>(this T existing, T value) where T : IMayHaveID
        {
            if (existing?.GetID() != value?.GetID())
            {
                //Only update if ID's matches
                return;
            }

            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var old = property.GetValue(existing);
                var @new = property.GetValue(value);
                var toSave = @new ?? old;
                property.SetValue(existing, toSave);
            }
        }
    }
}
