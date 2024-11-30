namespace ReadWriteMemory.Shared.Entities;

public sealed record CodeCaveTable
{
	public CodeCaveTable(byte[] originalOpcode, nuint caveAddress, byte[] jmpBytes)
	{
		OriginalOpcodes = originalOpcode;
		CaveAddress = caveAddress;
		JmpBytes = jmpBytes;
	}

	public nuint CaveAddress { get; }

	public byte[] OriginalOpcodes { get; }

	public byte[] JmpBytes { get; }
}