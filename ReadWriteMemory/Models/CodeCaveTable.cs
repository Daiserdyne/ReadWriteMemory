namespace ReadWriteMemory.Models;

internal sealed class CodeCaveTable
{
	public CodeCaveTable(byte[] originalOpcode, UIntPtr caveAddress)
	{
		OriginalOpcode = originalOpcode;
		CaveAddress = caveAddress;
	}

	public UIntPtr CaveAddress { get; }
	public byte[] OriginalOpcode { get; }
}