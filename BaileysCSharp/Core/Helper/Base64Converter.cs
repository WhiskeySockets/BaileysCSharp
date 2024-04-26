using System.Text.Json;
using System.Text.Json.Serialization;

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
}