namespace ReadWriteMemory.Models;

internal sealed record ProcessState
{
    internal bool IsProcessAlive { get; set; }

    internal readonly CancellationTokenSource ProcessStateTokenSrc = new();
}