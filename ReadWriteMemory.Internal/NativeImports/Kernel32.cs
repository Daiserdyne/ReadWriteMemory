using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Global

#pragma warning disable CS1591

namespace ReadWriteMemory.Internal.NativeImports;

public static partial class Kernel32
{
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllocConsole();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint VirtualAlloc(
        nint lpAddress,
        uint dwSize,
        AllocationType flAllocationType,
        MemoryProtection flProtect);
    
    [Flags]
    internal enum AllocationType : uint
    {
        Commit = 0x00001000,
        Reserve = 0x00002000,
    }

    [Flags]
    internal enum MemoryProtection : uint
    {
        ExecuteReadWrite = 0x40
    }
}