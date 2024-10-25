using System.Diagnostics;

namespace ReadWriteMemory.Models;

/// <summary>
/// Holds information about the current opened process.
/// </summary>
internal sealed record ProcessInformation
{
    internal string ProcessName { get; init; } = string.Empty;

    internal Process Process { get; set; } = new();

    internal nint Handle { get; set; } = nint.Zero;

    internal ProcessState ProcessState { get; } = new();

    internal Dictionary<string, nuint> Modules { get; } = [];
}