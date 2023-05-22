using System.Runtime.InteropServices;

namespace ReadWriteMemory.NativeImports;

internal static class Kernel32
{
    #region Constants

    // privileges
    internal const int FULL_MEMORY_ACCESS = 0x1F0FFF;
    internal const int PROCESS_CREATE_THREAD = 0x0002;
    internal const int PROCESS_QUERY_INFORMATION = 0x0400;
    internal const int PROCESS_VM_OPERATION = 0x0008;
    internal const int PROCESS_VM_WRITE = 0x0020;
    internal const int PROCESS_VM_READ = 0x0010;

    // used for memory allocation
    internal const uint MEM_FREE = 0x10000;
    internal const uint MEM_COMMIT = 0x00001000;
    internal const uint MEM_RESERVE = 0x00002000;
    internal const uint PAGE_READONLY = 0x02;
    internal const uint PAGE_READWRITE = 0x04;
    internal const uint PAGE_WRITECOPY = 0x08;
    internal const uint PAGE_EXECUTE_READWRITE = 0x40;
    internal const uint PAGE_EXECUTE_WRITECOPY = 0x80;
    internal const uint PAGE_EXECUTE = 0x10;
    internal const uint PAGE_EXECUTE_READ = 0x20;
    internal const uint PAGE_GUARD = 0x100;
    internal const uint PAGE_NOACCESS = 0x01;
    internal const uint MEM_PRIVATE = 0x20000;
    internal const uint MEM_IMAGE = 0x1000000;
    internal const uint MEM_RELEASE = 0x8000;
    internal const uint MEM_MAPPED = 0x40000;

    #endregion

    [DllImport("kernel32.dll")]
    internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    internal static IntPtr OpenProcess(bool bInheritHandle, int dwProcessId)
    {
        return OpenProcess(FULL_MEMORY_ACCESS, bInheritHandle, dwProcessId);
    }

    internal static UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer)
    {
        MEMORY_BASIC_INFORMATION64 tmp64 = new();
        var retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64, new UIntPtr((uint)Marshal.SizeOf(tmp64)));

        lpBuffer.BaseAddress = tmp64.BaseAddress;
        lpBuffer.AllocationBase = tmp64.AllocationBase;
        lpBuffer.AllocationProtect = tmp64.AllocationProtect;
        lpBuffer.RegionSize = (long)tmp64.RegionSize;
        lpBuffer.State = tmp64.State;
        lpBuffer.Protect = tmp64.Protect;
        lpBuffer.Type = tmp64.Type;

        return retVal;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int VirtualQueryEx(
    IntPtr hProcess,
    nuint lpAddress,
    out MEMORY_BASIC_INFORMATION lpBuffer,
    uint dwLength
);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int VirtualQueryEx(
IntPtr hProcess,
nuint lpAddress,
out MEMORY_BASIC_INFORMATION64 lpBuffer,
uint dwLength
);

    [Flags]
    public enum MemoryProtection : uint
    {
        NoAccess = 0x00000001,
        ReadOnly = 0x00000002,
        ReadWrite = 0x00000004,
        WriteCopy = 0x00000008,
        Execute = 0x00000010,
        ExecuteRead = 0x00000020,
        ExecuteReadWrite = 0x00000040,
        ExecuteWriteCopy = 0x00000080,
        Guard = 0x00000100,
        NoCache = 0x00000200,
        WriteCombine = 0x00000400,
        TargetsInvalid = 0x40000000,
        TargetsNoUpdate = 0x40000000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtectEx(
    IntPtr hProcess,
    UIntPtr lpAddress,
    UIntPtr dwSize,
    MemoryProtection flNewProtect,
    out uint lpflOldProtect
);


    [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    internal static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);

    [DllImport("kernel32.dll")]
    internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern bool VirtualFreeEx(
        IntPtr hProcess,
        UIntPtr lpAddress,
        UIntPtr dwSize,
        uint dwFreeType
        );

    [DllImport("kernel32.dll")]
    internal static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern UIntPtr VirtualAllocEx(
        IntPtr hProcess,
        UIntPtr lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
    );

    [DllImport("kernel32.dll")]
    internal static extern int CloseHandle(
    IntPtr hObject
    );

    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, nuint nSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32")]
    internal static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

    internal struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        ushort reserved;
        public uint pageSize;
        public UIntPtr minimumApplicationAddress;
        public UIntPtr maximumApplicationAddress;
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    internal struct MEMORY_BASIC_INFORMATION64
    {
        public UIntPtr BaseAddress;
        public UIntPtr AllocationBase;
        public uint AllocationProtect;
        public uint __alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint __alignment2;
    }

    internal struct MEMORY_BASIC_INFORMATION
    {
        public UIntPtr BaseAddress;
        public UIntPtr AllocationBase;
        public uint AllocationProtect;
        public long RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }
}