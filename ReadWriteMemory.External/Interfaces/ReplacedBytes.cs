namespace ReadWriteMemory.External.Interfaces;

internal readonly record struct ReplacedBytes
{
    internal readonly byte[] OriginalOpcodes { get; init; }
}