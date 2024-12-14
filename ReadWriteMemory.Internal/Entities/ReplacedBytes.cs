namespace ReadWriteMemory.Internal.Entities;

internal readonly record struct ReplacedBytes
{
    internal readonly byte[] OriginalOpcodes { get; init; }
}