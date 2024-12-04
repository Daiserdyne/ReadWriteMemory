using System.Runtime.InteropServices;

#pragma warning disable CS1591

namespace ReadWriteMemory.Internal.NativeImports;

public static class Kernel32
{
    [DllImport("kernel32")]
    public static extern bool AllocConsole();
}