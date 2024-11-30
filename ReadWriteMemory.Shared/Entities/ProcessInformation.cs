using System.Diagnostics;

namespace ReadWriteMemory.Shared.Entities;

/// <summary>
/// Holds information about the current opened process.
/// </summary>
public sealed record ProcessInformation
{
    public string ProcessName { get; init; } = string.Empty;

    public Process Process { get; set; } = new();

    public nint Handle { get; set; } = nint.Zero;

    public bool IsProcessAlive { get; set; }

    public Dictionary<string, nuint> Modules { get; } = [];
}