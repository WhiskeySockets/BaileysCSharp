using Google.Protobuf;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Helper
{
    public class ByteStringConverter : JsonConverter<ByteString>
    {
        public override ByteString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }


        public override void Write(Utf8JsonWriter writer, ByteString value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteBase64StringValue(value.ToByteArray());
        }

    }
}