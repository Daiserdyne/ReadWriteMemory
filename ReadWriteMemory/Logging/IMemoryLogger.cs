namespace ReadWriteMemory.Logging;

internal interface IMemoryLogger : IDisposable
{
    void Info(string caption, string message);
    void Warn(string caption, string message);
    void Error(string caption, string message);
    void Fatal(string caption, string message);
    void Debug(string caption, string message);
}