using System.Runtime.InteropServices;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace ReadWriteMemory.External.NativeImports;

internal static class Kernel32
{
    // privileges
    private const int FullMemoryAccess = 0x1F0FFF;

    // used for memory allocation
    internal const uint MemCommit = 0x00001000;
    internal const uint MemReserve = 0x00002000;
    internal const uint PageExecuteReadwrite = 0x40;
    internal const uint MemRelease = 0x8000;

    internal static nint OpenProcess(bool bInheritHandle, int dwProcessId)
    {
        return OpenProcess(FullMemoryAccess, bInheritHandle, dwProcessId);
    }
    
    [DllImport("kernel32.dll")]
    private static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    
    [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    private static extern nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
        out MemoryBasicInformation64 lpBuffer, nuint dwLength);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern bool VirtualFreeEx(
        nint hProcess,
        nuint lpAddress,
        nuint dwSize,
        uint dwFreeType
    );

    [DllImport("kernel32.dll")]
    internal static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, [Out] byte[] lpBuffer,
        int nSize, nint lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern nuint VirtualAllocEx(
        nint hProcess,
        nuint lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
    );

    [DllImport("kernel32.dll")]
    internal static extern int CloseHandle(
        nint hObject
    );

    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, nuint nSize,
        out nint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, int nSize,
        out nint lpNumberOfBytesWritten);

    [DllImport("kernel32")]
    internal static extern bool IsWow64Process(nint hProcess, out bool lpSystemInfo);

    private struct MemoryBasicInformation64
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public uint Alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint Alignment2;
    }
}