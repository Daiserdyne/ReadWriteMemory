namespace ReadWriteMemory.Models;

public sealed class MemoryAddress
{
    public MemoryAddress(int address, string moduleName, params int[]? offsets)
    {
        Address = address;
        ModuleName = moduleName;
        Offsets = offsets;
    }

    public int Address { get; }
    public string ModuleName { get; } = string.Empty;
    public int[]? Offsets { get; }
}