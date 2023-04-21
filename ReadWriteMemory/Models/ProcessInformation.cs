using System.Diagnostics;

namespace ReadWriteMemory.Models;

/// <summary>
/// Holds information about the current opened process.
/// </summary>
internal sealed record ProcessInformation
{
    internal string ProcessName { get; set; } = string.Empty;

    internal Process Process { get; set; } = new();

    internal IntPtr Handle { get; set; } = IntPtr.Zero;

    internal ProcessState ProcessState { get; set; } = new();

    internal ProcessModule? MainModule { get; set; }

    internal IDictionary<string, IntPtr>? Modules { get; set; }
}