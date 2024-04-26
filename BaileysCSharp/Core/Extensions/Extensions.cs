using Google.Protobuf;
using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Extensions
{
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
            if (info == null)
                return 0;
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
                if (property.SetMethod != null)
                {
                    try
                    {
                        property.SetValue(existing, toSave);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        public static TResult? FindMatchingValue<TSource, TResult>(this TSource source, string key)
        {
            if (source == null)
                return default(TResult);
            var sourceProperty = source.GetType().GetProperty(key);
            var value = sourceProperty?.GetValue(source, null); 
            if (value == null)
                return default(TResult);
            return (TResult)value;
        }

        public static void CopyMatchingValues<TDestination, TSource>(this TDestination destination, TSource source)
        {
            if (source == null)
                return;
            if (destination == null)
                return;
            var properties = destination.GetType().GetProperties();
            foreach (var destinationProperty in properties)
            {
                var sourceProperty = source.GetType().GetProperty(destinationProperty.Name);
                if (sourceProperty != null)
                {
                    var value = sourceProperty.GetValue(source, null);
                    if (value != null)
                    {
                        if (sourceProperty.PropertyType == destinationProperty.PropertyType)
                        {
                            destinationProperty.SetValue(destination, value, null);
                        }

                        if (value is byte[] buffer && destinationProperty.PropertyType == typeof(ByteString))
                        {
                            destinationProperty.SetValue(destination, buffer.ToByteString(), null);
                        }

                        if (value is ByteString bytebuffer && destinationProperty.PropertyType == typeof(byte[]))
                        {
                            destinationProperty.SetValue(destination, bytebuffer.ToByteArray(), null);
                        }
                    }
                }

            }

        }


    }
}
