namespace ReadWriteMemory.Logging;

public sealed class MemoryLogger : IDisposable
{
    public delegate void MemLogger(LoggingType type, string message);
    public event MemLogger? MemoryLogger_OnLogging;

    /// <summary>
    /// Shows which type of log you get.
    /// </summary>
    public enum LoggingType : short
    {
        Info,
        Warn,
        Error
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

/// <inheritdoc/>
    public void Dispose()
    {
        MemoryLogger_OnLogging = null;
    }
}