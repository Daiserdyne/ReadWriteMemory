namespace ReadWriteMemory.Shared.Entities;

/// <summary>
/// Contains/holds some information about the target memory address address.
/// </summary>
public sealed record MemoryAddressTable
{
    public nuint BaseAddress { get; init; } = nuint.Zero;

    public CodeCaveTable? CodeCaveTable { get; set; }

    public ReplacedBytes? ReplacedBytes { get; set; }
    
    public CancellationTokenSource? FreezeTokenSrc { get; set; }
    
    public CancellationTokenSource? ReadValueConstantTokenSrc { get; set; }
}