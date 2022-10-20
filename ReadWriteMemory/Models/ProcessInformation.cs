using System.Diagnostics;

namespace ReadWriteMemory.Models;

/// <summary>
/// Information about the current opened process.
/// </summary>
internal sealed class ProcessInformation
{
    internal string ProcessName { get; set; } = string.Empty;
    internal Process Process { get; set; } = new();
    internal IntPtr Handle { get; set; }
    internal ProcessModule? MainModule { get; set; }
}