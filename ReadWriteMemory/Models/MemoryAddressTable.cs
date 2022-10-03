namespace ReadWriteMemory.Models;

/// <summary>
/// Contains a sort of information table of a address.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class MemoryAddressTable
{
    public int Address { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int[] Offsets { get; set; } = new int[0];
    public UIntPtr BaseAddress { get; set; }
    public string UniqueAddressHash { get; set; } = string.Empty;
    public CodeCaveTable? CodeCaveTable { get; set; }
}