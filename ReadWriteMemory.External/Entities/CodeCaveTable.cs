namespace ReadWriteMemory.External.Entities;

/// <summary>
/// 
/// </summary>
public readonly record struct CodeCaveTable
{
    internal CodeCaveTable(byte[] originalOpcode, nuint caveAddress, uint sizeOfAllocatedMemory, byte[] jmpBytes)
    {
        OriginalOpcodes = originalOpcode;
        CaveAddress = caveAddress;
        JmpBytes = jmpBytes;
        SizeOfAllocatedMemory = sizeOfAllocatedMemory;
    }

    /// <summary>
    /// 
    /// </summary>
    public static CodeCaveTable Empty { get; } = new([], nuint.Zero, 0, []);
	
    /// <summary>
    /// 
    /// </summary>
    public nuint CaveAddress { get; }
	
    /// <summary>
    /// 
    /// </summary>
    public uint SizeOfAllocatedMemory { get; }
	
    /// <summary>
    /// 
    /// </summary>
    public byte[] OriginalOpcodes { get; }
	
    /// <summary>
    /// 
    /// </summary>
    public byte[] JmpBytes { get; }
}