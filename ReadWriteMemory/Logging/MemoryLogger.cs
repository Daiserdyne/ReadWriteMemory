namespace ReadWriteMemory.Logging;

public sealed class MemoryLogger : IDisposable
{
    public delegate void NotifyWhenLogging(string caption, string message);
    public event NotifyWhenLogging? OnLogging;

    internal void Debug(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Debug]: {message}");
    }

    internal void Error(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Error]: {message}");
    }

    internal void Fatal(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Fatal]: {message}");
    }

    internal void Info(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Info]: {message}");
    }

    internal void Warn(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Warn]: {message}");
    }

    public void Dispose()
    {
        OnLogging = null;
    }
}