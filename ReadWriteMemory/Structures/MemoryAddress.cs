namespace ReadWriteMemory.Structures;

/// <summary>
/// Contains a sort of information table of a address.
/// </summary>
/// <typeparam name="T"></typeparam>
internal record struct MemoryAddress
{
    public int Address { get; set; }
    public string ModuleName { get; set; }
    public int[] Offsets { get; set; }
    public UIntPtr BaseAddress { get; set; }
    public string UniqueAddressHash { get; set; }
}