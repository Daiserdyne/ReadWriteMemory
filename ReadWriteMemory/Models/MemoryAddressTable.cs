namespace ReadWriteMemory.Models;

/// <summary>
/// Contains/holds some information about the target memory address address.
/// </summary>
internal sealed record MemoryAddressTable
{
    internal MemoryAddress? MemoryAddress { get; set; }

    internal nuint BaseAddress { get; set; } = nuint.Zero;

    internal CodeCaveTable? CodeCaveTable { get; set; }

    internal CancellationTokenSource? FreezeTokenSrc { get; set; }
}