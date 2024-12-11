using System.Runtime.InteropServices;

#pragma warning disable CS1591

namespace ReadWriteMemory.Internal.NativeImports;

public static class Kernel32
{
    [DllImport("kernel32")]
    public static extern bool AllocConsole();
    
    [DllImport("kernel32")]
    public static extern void FreeLibraryAndExitThread(nint hLibModule, int dwExitCode);
    
    [DllImport("kernel32")]
    public static extern bool FreeLibrary(nint hLibModule);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint CreateThread(
        nint lpThreadAttributes,
        uint dwStackSize,
        nint lpStartAddress,
        nint lpParameter,
        uint dwCreationFlags,
        out uint lpThreadId
    );
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern void ExitThread(uint dwExitCode);
}