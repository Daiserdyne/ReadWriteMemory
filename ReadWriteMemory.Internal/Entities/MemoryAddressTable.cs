namespace ReadWriteMemory.Internal.Entities;

/// <summary>
/// Contains/holds some information about the target memory address.
/// </summary>
internal sealed record MemoryAddressTable
{
    internal nuint BaseAddress { get; init; } = nuint.Zero;

    internal CodeCaveTable? CodeCaveTable { get; set; }

    internal ReplacedBytes? ReplacedBytes { get; set; }
    
    internal CancellationTokenSource? FreezeTokenSrc { get; set; }
    
    internal CancellationTokenSource? ReadValueConstantTokenSrc { get; set; }
}