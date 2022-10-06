namespace ReadWriteMemory.Models;

public sealed class MemoryAddress
{
    public MemoryAddress(long address, string moduleName = "", params int[]? offsets)
    {
        Address = address;
        ModuleName = moduleName;
        Offsets = offsets;
    }

    public MemoryAddress(long address, params int[] offsets)
    {
        Address = address;
        ModuleName = string.Empty;
        Offsets = offsets;
    }

    public long Address { get; }
    public string ModuleName { get; }
    public int[]? Offsets { get; }
}