namespace ReadWriteMemory.Internal.Entities;

public readonly record struct CodeCaveTable
{
	internal CodeCaveTable(byte[] originalOpcode, nuint caveAddress, uint sizeOfAllocatedMemory, byte[] jmpBytes)
	{
		OriginalOpcodes = originalOpcode;
		CaveAddress = caveAddress;
		JmpBytes = jmpBytes;
		SizeOfAllocatedMemory = sizeOfAllocatedMemory;
	}

	public static CodeCaveTable Empty { get; } = new([], nuint.Zero, 0, []);
	
	public nuint CaveAddress { get; }
	
	public uint SizeOfAllocatedMemory { get; }
	
	public byte[] OriginalOpcodes { get; }
	
	public byte[] JmpBytes { get; }
}