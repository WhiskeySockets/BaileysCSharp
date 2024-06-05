using BaileysCSharp.Core.Helper;
using Google.Protobuf;
using Proto;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

namespace BaileysCSharp.Core.Logging
{
    public class DefaultLogger : ILogger
    {
        public DefaultLogger()
        {

        }

        private static object locker = new object();
        public LogLevel Level { get; set; }


        public void Error(string message)
        {
            if (Level <= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };
                lock (locker)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Write(logEntry);
                    Console.ResetColor();
                }
            }
        }
        public void Error(object? obj, string message)
        {
            if (Level <= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Write(logEntry);
                    Console.ResetColor();
                }
            }
        }

        public void Error(Exception ex, string message)
        {
            if (Level <= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    error = ex.Message,
                    message
                };

                lock (locker)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Write(logEntry);
                    Console.ResetColor();
                }
            }
        }

        public void Warn(object? obj, string message)
        {
            if (Level <= LogLevel.Warn)
            {
                var logEntry = new
                {
                    level = LogLevel.Warn,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };
                lock (locker)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Write(logEntry);
                    Console.ResetColor();
                }
            }
        }

        public void Warn(string message)
        {
            if (Level <= LogLevel.Warn)
            {
                var logEntry = new
                {
                    level = LogLevel.Warn,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Write(logEntry);
                    Console.ResetColor();
                }
            }
        }

        public void Info(string message)
        {
            if (Level <= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Info(object? obj, string message)
        {
            if (Level <= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }
        public void Debug(string message)
        {
            if (Level <= LogLevel.Debug)
            {
                var logEntry = new
                {
                    level = LogLevel.Debug,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Debug(object? obj, string message)
        {
            if (Level <= LogLevel.Debug)
            {
                var logEntry = new
                {
                    level = LogLevel.Debug,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Trace(string message)
        {
            if (Level <= LogLevel.Trace)
            {
                var logEntry = new
                {
                    level = LogLevel.Trace,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Trace(object? obj, string message)
        {
            if (Level <= LogLevel.Trace)
            {
                var logEntry = new
                {
                    level = LogLevel.Trace,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }


        private void Write(object logEntry)
        {
            var settings = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new Base64Converter(), new ByteStringConverter(), new ProtoConverterFactory() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var json = JsonSerializer.Serialize(logEntry, settings);
            System.Diagnostics.Debug.WriteLine(json);
            Console.Write($"{json}\n");
        }

        public void Raw(object obj, string message)
        {
            if (Level >= LogLevel.Raw)
            {
                var logEntry = new
                {
                    level = LogLevel.Raw,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }
    }
}