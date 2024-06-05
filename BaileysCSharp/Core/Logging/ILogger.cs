namespace BaileysCSharp.Core.Logging
{
    public interface ILogger
    {
        LogLevel Level { get; set; }

        void Debug(object? obj, string message);
        void Debug(string message);
        void Error(Exception ex, string message);
        void Error(object? obj, string message);
        void Error(string message);
        void Info(object? obj, string message);
        void Info(string message);
        void Raw(object obj, string message);
        void Trace(object? obj, string message);
        void Trace(string message);
        void Warn(object? obj, string message);
        void Warn(string message);
    }
}