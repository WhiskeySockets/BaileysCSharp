using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Remoting;

namespace BaileysCSharp.Core.Converters
{
    public class InterfaceConverter<T> : JsonConverter<T> where T : class
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader readerClone = reader;
            if (readerClone.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Problem in Start object! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Token Type not equal to property name! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            string? propertyName = readerClone.GetString();
            if (string.IsNullOrWhiteSpace(propertyName) || propertyName != "$type")
                throw new JsonException("Unable to get $type! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.String)
                throw new JsonException("Token Type is not JsonTokenString! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            string? typeValue = readerClone.GetString();
            if (string.IsNullOrWhiteSpace(typeValue))
                throw new JsonException("typeValue is null or empty string! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            string? asmbFullName = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => !string.IsNullOrEmpty(ass.GetName().Name) && ass.GetName().Name.Equals(typeValue.Split(" ")[1]))?.FullName;

            if (string.IsNullOrWhiteSpace(asmbFullName))
                throw new JsonException("Assembly name is null or empty string! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            ObjectHandle? instance = Activator.CreateInstance(asmbFullName, typeValue.Split(" ")[0]);
            if (instance == null)
                throw new JsonException("Unable to create object handler! Handler is null! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            object? unwrapedInstance = instance.Unwrap();
            if (unwrapedInstance == null)
                throw new JsonException("Unable to unwrap instance! Or instance is null! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            Type? entityType = unwrapedInstance.GetType();
            if (entityType == null)
                throw new JsonException("Instance type is null! Or instance is null! method: " + nameof(Read) + " class :" + nameof(InterfaceConverter<T>));

            object? deserialized = JsonSerializer.Deserialize(ref reader, entityType, options);
            if (deserialized == null)
                throw new JsonException("De-Serialized object is null here!");

            return (T)deserialized;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, typeof(T), options);
                    break;
                default:
                    {
                        var type = value.GetType();
                        using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(value, type, options));
                        writer.WriteStartObject();
                        //writer.WriteString("$type", type.FullName + " " + type.Assembly.GetName().Name);

                        foreach (var element in jsonDocument.RootElement.EnumerateObject())
                        {
                            element.WriteTo(writer);
                        }

                        writer.WriteEndObject();
                        break;
                    }
            }
        }
    }
}
