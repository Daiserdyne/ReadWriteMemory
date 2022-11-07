namespace ReadWriteMemory.Models;

internal sealed class CodeCaveTable
{
    internal CodeCaveTable(byte[] originalOpcode, UIntPtr caveAddress, byte[] jmpBytes)
	{
		OriginalOpcodes = originalOpcode;
		CaveAddress = caveAddress;
		JmpBytes = jmpBytes;
	}

	internal UIntPtr CaveAddress { get; }
	internal byte[] OriginalOpcodes { get; }
    internal byte[] JmpBytes { get; }
	//internal bool IsPaused { get; set; }
}