﻿// ReSharper disable RedundantReadonlyModifier
namespace ReadWriteMemory.External.Entities;

internal readonly record struct ReplacedBytes
{
    internal readonly byte[] OriginalOpcodes { get; init; }
}