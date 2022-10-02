namespace ReadWriteMemory.Structures;

/// <summary>
/// AoB scan information.
/// </summary>
internal struct MemoryRegionResult
{
    public UIntPtr CurrentBaseAddress { get; set; }
    public long RegionSize { get; set; }
    public UIntPtr RegionBase { get; set; }
}