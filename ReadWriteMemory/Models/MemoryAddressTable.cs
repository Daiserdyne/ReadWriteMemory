namespace ReadWriteMemory.Models;

/// <summary>
/// Contains a sort of information table of a address.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class MemoryAddressTable
{
    public MemoryAddress? MemoryAddress { get; set; }
    public UIntPtr BaseAddress { get; set; }
    public string UniqueAddressHash { get; set; } = string.Empty;
    public CodeCaveTable? CodeCaveTable { get; set; }
}