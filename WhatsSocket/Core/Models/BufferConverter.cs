using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    internal class BufferConverter : JsonConverter<byte[]>
    {
        public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                existingValue = Convert.FromBase64String(reader.Value.ToString());
                return existingValue;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonException();
            }

            //Read first property
            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName)
            {
                //Make sure it has a property 'type'
                if (reader.Value?.ToString() == "type")
                {
                    var value = reader.ReadAsString();
                    if (value == "Buffer")
                    {
                        //Read the data value
                        reader.Read();
                        value = reader.ReadAsString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            existingValue = Convert.FromBase64String(value);
                        }
                    }
                    else
                    {
                        throw new JsonException("the type is not defined as a Buffer");
                    }
                }

            }
            reader.Read();


            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
        {
            var data = JsonConvert.SerializeObject(new
            {
                type = "Buffer",
                data = Convert.ToBase64String(value)
            });

            //writer.WriteStartObject();

            writer.WriteRawValue(data);

            //writer.WriteEndObject();
        }
    }


}
