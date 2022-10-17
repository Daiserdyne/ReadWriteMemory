namespace ReadWriteMemory.Logging;

public sealed class MemoryLogger : IDisposable
{
    public delegate void MemLogger(LoggingType type, string message);
    public event MemLogger? MemoryLogger_OnLogging;

    public enum LoggingType : short
    {
        Info,
        Warn,
        Error,
        Debug
    }

    internal void Info(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LoggingType.Info, message);
    }

    internal void Warn(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LoggingType.Warn, message);
    }

    internal void Error(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LoggingType.Error, message);
    }

    internal void Debug(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LoggingType.Debug, message);
    }

    public void Dispose()
    {
        MemoryLogger_OnLogging = null;
    }
}