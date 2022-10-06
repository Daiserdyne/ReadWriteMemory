namespace ReadWriteMemory.Models;

internal sealed class CodeCaveTable
{
	public CodeCaveTable(byte[] originalOpcode, UIntPtr caveAddress, byte[] jmpBytes)
	{
		OriginalOpcodes = originalOpcode;
		CaveAddress = caveAddress;
		JmpBytes = jmpBytes;
	}

	public UIntPtr CaveAddress { get; }
	public byte[] OriginalOpcodes { get; }
	public byte[] JmpBytes { get; }
}