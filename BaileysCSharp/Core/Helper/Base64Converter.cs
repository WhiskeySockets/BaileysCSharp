using BaileysCSharp.Core.WABinary;
using Google.Protobuf;
using Proto;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace BaileysCSharp.Core.Helper
{
    public class Base64Converter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteBase64StringValue(value);
        }
    }

    public class ProtoConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var interfaces = typeToConvert.GetInterfaces();
            if (interfaces.Count() > 0)
            {
                foreach (var iface in interfaces)
                {
                    if (iface == typeof(IMessage))
                        return true;
                }
            }

            if (typeToConvert == typeof(BinaryNode))
            {
                return true;
            }

            if (typeToConvert == typeof(DateTime))
            {
                return true;
            }

            return false;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var interfaces = typeToConvert.GetInterfaces();
            if (interfaces.Count() > 0)
            {
                foreach (var iface in interfaces)
                {
                    if (iface == typeof(IMessage))
                        return new IProtoConverter();
                }
            }

            if (typeToConvert == typeof(BinaryNode))
            {
                return new IBinaryNodeConverter();
            }

            if (typeToConvert == typeof(DateTime))
            {
                return new DateTimeConverter();
            }

            

            throw new NotImplementedException();
        }


        public class IBinaryNodeConverter : JsonConverter<BinaryNode>
        {
            public override BinaryNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, BinaryNode value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }
                var xml = BinaryNodeToString(value);
                writer.WriteStringValue(Encoding.UTF8.GetBytes(xml));
            }


            private string BinaryNodeToString(BinaryNode frame, int i = 0)
            {
                StringBuilder builder = new StringBuilder();


                builder.Append($"<{frame.tag}");
                foreach (var item in frame.attrs)
                {
                    builder.Append($" {item.Key}='{item.Value}'");
                }

                if (frame.content != null)
                {
                    builder.Append($">");


                    if (frame.content is byte[])
                    {
                        builder.Append(Tabs(i)).Append(Convert.ToBase64String(frame.ToByteArray()));
                    }
                    if (frame.content is BinaryNode[] children)
                    {
                        foreach (var item in children)
                        {
                            builder.AppendLine();
                            builder.Append(Tabs(i + 1)).Append(BinaryNodeToString(item, i + 1));
                        }
                    }
                    builder.Append($"<{frame.tag}>");
                }
                else
                {
                    builder.Append($"/>");
                }
                var xmlNode = builder.ToString().Replace("\r", "");
                return xmlNode;
            }

            private static string Tabs(int indent)
            {
                var tabs = "";
                for (int i = 0; i < indent; i++)
                {
                    tabs = tabs + "\t";
                }
                return tabs;
            }
        }

        public class IProtoConverter : JsonConverter<IMessage>
        {
            public override IMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, IMessage value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }

                var json = value.ToString();
                if (json == null)
                {
                    writer.WriteNullValue();
                    return;
                }
                writer.WriteRawValue(json);
            }
        }



        public class DateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                var json = value.ToString("yyyy-MM-dd HH:mm:ss");
                if (json == null)
                {
                    writer.WriteNullValue();
                    return;
                }
                writer.WriteStringValue(json);
            }
        }
    }

}