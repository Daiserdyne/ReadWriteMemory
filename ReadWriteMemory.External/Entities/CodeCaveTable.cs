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
    /// This is the default/empty state of the <see cref="CodeCaveTable"/> struct.
    /// </summary>
    public static CodeCaveTable Empty { get; } = new([], nuint.Zero, 0, []);
	
    /// <summary>
    /// <see cref="CaveAddress"/> points to the new allocated area in memory where
    /// your custom code is located. 
    /// </summary>
    public nuint CaveAddress { get; }
	
    /// <summary>
    /// The amount of new allocated memory in the process.
    /// </summary>
    public uint SizeOfAllocatedMemory { get; }
	
    /// <summary>
    /// These are the original opcodes of your function you have overwritten with
    /// the opcodes who are pointing to the codecave address.
    /// </summary>
    public byte[] OriginalOpcodes { get; }
	
    /// <summary>
    /// These are the bytes who make the jump to the cave address.
    /// </summary>
    public byte[] JmpBytes { get; }
}