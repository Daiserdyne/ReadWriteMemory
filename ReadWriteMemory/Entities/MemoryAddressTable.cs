namespace ReadWriteMemory.Entities;

/// <summary>
/// Contains/holds some information about the target memory address address.
/// </summary>
internal sealed record MemoryAddressTable
{
    internal nuint BaseAddress { get; init; } = nuint.Zero;

    internal CodeCaveTable? CodeCaveTable { get; set; }

    internal CancellationTokenSource? FreezeTokenSrc { get; set; }
    
    internal CancellationTokenSource? ReadValueConstantTokenSrc { get; set; }
}