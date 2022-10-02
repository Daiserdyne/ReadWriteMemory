using System.Diagnostics;

namespace ReadWriteMemory.Models;

/// <summary>
/// Information about the current opened process.
/// </summary>
internal sealed class ProcessInformation
{
    public string ProcessName { get; set; } = string.Empty;
    public Process Process { get; set; } = new();
    public IntPtr Handle { get; set; }
    public bool Is64Bit { get; set; }
    public ProcessModule? MainModule { get; set; }
}