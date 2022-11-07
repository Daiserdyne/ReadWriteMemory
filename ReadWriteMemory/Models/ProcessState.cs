namespace ReadWriteMemory.Models;

internal class ProcessState
{
    internal bool CurrentProcessState { get; set; }
#pragma warning disable CA1822
    internal CancellationTokenSource ProcessStateTokenSrc => new();
#pragma warning restore CA1822
}