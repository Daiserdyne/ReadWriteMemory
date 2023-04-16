namespace ReadWriteMemory.Models;

/// <summary>
/// Contains/holds some information about the target memory address address.
/// </summary>
internal sealed record MemoryAddressTable
{
    internal MemoryAddress? MemoryAddress { get; set; }

    internal UIntPtr BaseAddress { get; set; } = UIntPtr.Zero;

    internal string UniqueAddressHash { get; set; } = string.Empty;

    internal CodeCaveTable? CodeCaveTable { get; set; }

    internal CancellationTokenSource? FreezeTokenSrc { get; set; }

    // Für callback methods eine liste mit canceltoken anlegen, damit man nicht mehrmals einen background task startet.
}