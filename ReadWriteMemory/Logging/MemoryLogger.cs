namespace ReadWriteMemory.Logging;

public sealed class MemoryLogger : IMemoryLogger
{
    public delegate void NotifyWhenLogging(string caption, string message);
    public event NotifyWhenLogging? OnLogging;

    public void Debug(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Debug]: {message}");
    }

    public void Error(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Error]: {message}");
    }

    public void Fatal(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Fatal]: {message}");
    }

    public void Info(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Info]: {message}");
    }

    public void Warn(string caption, string message)
    {
        OnLogging?.Invoke(caption, $"[{DateTime.Now}][Warn]: {message}");
    }

    public void Dispose()
    {
        OnLogging = null;
    }
}