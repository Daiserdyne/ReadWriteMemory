namespace ReadWriteMemory.Models;

internal sealed class CodeCaveTable
{
	public CodeCaveTable(byte[] originalOpcode, UIntPtr caveAddress, byte[] jmpBytes)
	{
		OriginalOpcode = originalOpcode;
		CaveAddress = caveAddress;
		JmpBytes = jmpBytes;
	}

	public UIntPtr CaveAddress { get; }
	public byte[] OriginalOpcode { get; }
	public byte[] JmpBytes { get; }
}