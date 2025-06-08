using System.Runtime.InteropServices;

#pragma warning disable CS0649

namespace ReadWriteMemory.External.NativeImports;

internal static partial class Kernel32
{
    // privileges
    internal const int FullMemoryAccess = 0x1F0FFF;

    // used for memory allocation
    internal const uint MemCommit = 0x00001000;
    internal const uint MemReserve = 0x00002000;
    internal const uint PageExecuteReadwrite = 0x40;
    internal const uint MemRelease = 0x8000;

    [LibraryImport("kernel32.dll")]
    internal static partial nint OpenProcess(
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VirtualFreeEx(
        nint hProcess,
        nuint lpAddress,
        nuint dwSize,
        uint dwFreeType
    );

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ReadProcessMemory(
        nint hProcess,
        nuint lpBaseAddress,
        [Out] byte[] lpBuffer,
        int nSize,
        nint lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint VirtualAllocEx(
        nint hProcess,
        nuint lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
    );

    [LibraryImport("kernel32.dll")]
    internal static partial int CloseHandle(nint hObject);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WriteProcessMemory(nint hProcess,
        nuint lpBaseAddress,
        ReadOnlySpan<byte> lpBuffer,
        nuint nSize,
        out nint lpNumberOfBytesWritten);

    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWow64Process(nint hProcess, [MarshalAs(UnmanagedType.Bool)] out bool lpSystemInfo);
}