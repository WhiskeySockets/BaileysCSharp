using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Tls;
using Proto;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace WhatsSocket.Core.Helper
{
    public enum LogLevel
    {
        Info = 0,
        Warn = 1,
        Error = 2,
        Fatal = 3,
        Verbose = 4,
        Debug = 5
    }

    public class Logger
    {
        public LogLevel Level { get; set; }

        internal void Trace(string message)
        {
            if (Level >= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };


                Write(logEntry);
            }
        }

        public void Trace(object? obj, string message)
        {
            if (Level >= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };


                Write(logEntry);
            }
        }
        internal void Error(string message)
        {
            if (Level >= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };
                Write(logEntry);
            }
        }

        public void Error(Exception ex, string message)
        {
            if (Level >= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    error = ex.Message,
                    message
                };


                Write(logEntry);
            }
        }

        internal void Info(string message)
        {
            if (Level >= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                Write(logEntry);
            }
        }

        internal void Debug(object? obj, string message)
        {
            if (Level >= LogLevel.Debug)
            {
                var logEntry = new
                {
                    level = LogLevel.Debug,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                Write(logEntry);
            }
        }

        internal void Info(object? obj, string message)
        {
            if (Level >= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                Write(logEntry);
            }
        }

        private void Write(object logEntry)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters = new List<JsonConverter>() { new Base64Converter(), new ByteStringConverter(), new Newtonsoft.Json.Converters.StringEnumConverter() };
            settings.ContractResolver = new ConditionalResolver();
            settings.NullValueHandling = NullValueHandling.Ignore;
            var json = JsonConvert.SerializeObject(logEntry, settings);
            System.Diagnostics.Debug.WriteLine(json);
            Console.WriteLine(json + "\n");
        }

        internal void Warn(string message)
        {
            var logEntry = new
            {
                level = LogLevel.Warn,
                time = DateTime.Now,
                hostname = Dns.GetHostName(),
                message
            };

            Write(logEntry);
        }
    }
    public class ConditionalResolver : DefaultContractResolver
    {

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            if (prop.PropertyName.StartsWith("Has") && prop.PropertyType == typeof(bool))
            {
                return null;
            }

            return prop;
        }
    }

    public class Base64Converter : JsonConverter<byte[]>
    {
        public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(Convert.ToBase64String(value));
        }
    }

    public class ByteStringConverter : JsonConverter<ByteString>
    {
        public override ByteString? ReadJson(JsonReader reader, Type objectType, ByteString? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, ByteString? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(Convert.ToBase64String(value.ToByteArray()));
        }
    }
}