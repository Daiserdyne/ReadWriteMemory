namespace ReadWriteMemory.Logging;

public sealed class MemoryLogger : IDisposable
{
    public delegate void MemLogger(LogType type, string message);
    public event MemLogger? MemoryLogger_OnLogging;

    public enum LogType : short
    {
        Info,
        Warn,
        Error,
        Debug
    }

    internal void Info(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LogType.Info, message);
    }

    internal void Warn(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LogType.Warn, message);
    }

    internal void Error(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LogType.Error, message);
    }

    internal void Debug(string message)
    {
        MemoryLogger_OnLogging?.Invoke(LogType.Debug, message);
    }

    public void Dispose()
    {
        MemoryLogger_OnLogging = null;
    }
}