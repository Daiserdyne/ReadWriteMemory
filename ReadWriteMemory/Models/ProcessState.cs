namespace ReadWriteMemory.Models;

internal class ProcessState
{
    internal bool CurrentProcessState { get; set; }
    internal static CancellationTokenSource ProcessStateTokenSrc => new();
}