namespace ReadWriteMemory.Entities;

internal sealed record CodeCaveTable
{
    internal CodeCaveTable(byte[] originalOpcode, nuint caveAddress, byte[] jmpBytes)
	{
		OriginalOpcodes = originalOpcode;
		CaveAddress = caveAddress;
		JmpBytes = jmpBytes;
	}

	internal nuint CaveAddress { get; }

	internal byte[] OriginalOpcodes { get; }

    internal byte[] JmpBytes { get; }
}