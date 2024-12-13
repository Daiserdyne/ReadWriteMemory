using System.Diagnostics;

namespace ReadWriteMemory.External.Entities;

/// <summary>
/// Holds information about the current opened process.
/// </summary>
internal sealed record ProcessInformation
{
    internal string ProcessName { get; init; } = string.Empty;

    internal Process Process { get; set; } = new();

    internal nint Handle { get; set; } = nint.Zero;

    internal bool IsProcessAlive { get; set; }

    internal Dictionary<string, nuint> Modules { get; } = [];
}