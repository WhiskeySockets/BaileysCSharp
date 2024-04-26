using System.Text.Json;
using System.Text.Json.Serialization;

namespace BaileysCSharp.Core.Converters
{
    internal class BufferConverter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return Convert.FromBase64String(value);
            }
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            reader.Read();

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                //Make sure it has a property 'type'
                if (reader.GetString() == "type")
                {
                    reader.Read();
                    var value = reader.GetString();
                    if (value == "Buffer")
                    {
                        //Read the data value
                        reader.Read();
                        value = reader.GetString();
                        if (value == "data")
                        {
                            reader.Read();
                            value = reader.GetString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                var result = Convert.FromBase64String(value);
                                reader.Read();
                                return result;
                            }
                        }
                        else
                        {
                            throw new JsonException("the data is not defined");
                        }

                    }
                    else
                    {
                        throw new JsonException("the type is not defined as a Buffer");
                    }
                }

            }

            return null;
        }

        //public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
        //{
        //    if (reader.TokenType == JsonToken.String)
        //    {
        //        existingValue = Convert.FromBase64String(reader.Value.ToString());
        //        return existingValue;
        //    }
        //
        //    if (reader.TokenType != JsonToken.StartObject)
        //    {
        //        throw new JsonException();
        //    }
        //
        //    //Read first property
        //    reader.Read();
        //    if (reader.TokenType == JsonToken.PropertyName)
        //    {
        //        //Make sure it has a property 'type'
        //        if (reader.Value?.ToString() == "type")
        //        {
        //            var value = reader.ReadAsString();
        //            if (value == "Buffer")
        //            {
        //                //Read the data value
        //                reader.Read();
        //                value = reader.ReadAsString();
        //                if (!string.IsNullOrEmpty(value))
        //                {
        //                    existingValue = Convert.FromBase64String(value);
        //                }
        //            }
        //            else
        //            {
        //                throw new JsonException("the type is not defined as a Buffer");
        //            }
        //        }
        //
        //    }
        //    reader.Read();
        //
        //
        //    return existingValue;
        //}

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            var data = JsonSerializer.Serialize(new
            {
                type = "Buffer",
                data = Convert.ToBase64String(value)
            });
            writer.WriteRawValue(data);
        }

    }


}
