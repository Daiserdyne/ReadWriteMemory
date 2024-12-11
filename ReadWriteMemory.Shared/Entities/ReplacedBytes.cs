namespace ReadWriteMemory.Shared.Entities;

public readonly record struct ReplacedBytes
{
    public readonly byte[] OriginalOpcodes { get; init; }
}