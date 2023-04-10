namespace ReadWriteMemory.Models;

internal sealed record ProcessState
{
    internal bool CurrentProcessState { get; set; }

    internal CancellationTokenSource ProcessStateTokenSrc => new();
}