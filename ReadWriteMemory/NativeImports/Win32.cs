﻿using System.Runtime.InteropServices;
using System.Text;

namespace ReadWriteMemory.NativeImports;

internal sealed class Win32
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
    internal const uint MEM_MAPPED = 0x40000;

    #endregion

    [DllImport("kernel32.dll")]
    internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    internal static IntPtr OpenProcess(bool bInheritHandle, int dwProcessId)
    {
        return OpenProcess(FULL_MEMORY_ACCESS, bInheritHandle, dwProcessId);
    }

#if WINXP
#else

    [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    internal static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);

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

    [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    internal static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);

    [DllImport("kernel32.dll")]
    internal static extern uint GetLastError();

    [DllImport("kernel32.dll")]
    internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

#endif

    [DllImport("kernel32.dll")]
    internal static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int SuspendThread(IntPtr hThread);
    [DllImport("kernel32.dll")]
    internal static extern int ResumeThread(IntPtr hThread);

    [DllImport("dbghelp.dll")]
    internal static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        int ProcessId,
        IntPtr hFile,
        MINIDUMP_TYPE DumpType,
        IntPtr ExceptionParam,
        IntPtr UserStreamParam,
        IntPtr CallackParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);

    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(
        IntPtr hProcess,
        UIntPtr lpBaseAddress,
        string lpBuffer,
        UIntPtr nSize,
        out IntPtr lpNumberOfBytesWritten
    );

    [DllImport("kernel32.dll")]
    internal static extern int GetProcessId(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint GetPrivateProfileString(
       string lpAppName,
       string lpKeyName,
       string lpDefault,
       StringBuilder lpReturnedString,
       uint nSize,
       string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern bool VirtualFreeEx(
        IntPtr hProcess,
        UIntPtr lpAddress,
        UIntPtr dwSize,
        uint dwFreeType
        );

    [DllImport("psapi.dll")]
    internal static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);
    [DllImport("psapi.dll", SetLastError = true)]
    internal static extern bool EnumProcessModules(IntPtr hProcess,
    [Out] IntPtr lphModule,
    uint cb,
    [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

    [DllImport("kernel32.dll")]
    internal static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    internal static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    internal static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] IntPtr lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern UIntPtr VirtualAllocEx(
        IntPtr hProcess,
        UIntPtr lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
    );

    [DllImport("kernel32.dll")]
    internal static extern bool VirtualProtectEx(IntPtr hProcess, UIntPtr lpAddress,
        IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    internal static extern UIntPtr GetProcAddress(
        IntPtr hModule,
        string procName
    );

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
    internal static extern bool _CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    internal static extern int CloseHandle(
    IntPtr hObject
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(
        string lpModuleName
    );

    [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
    internal static extern int WaitForSingleObject(
        IntPtr handle,
        int milliseconds
    );

    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

    // Added to avoid casting to UIntPtr
    [DllImport("kernel32.dll")]
    internal static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32")]
    internal static extern IntPtr CreateRemoteThread(
      IntPtr hProcess,
      IntPtr lpThreadAttributes,
      uint dwStackSize,
      UIntPtr lpStartAddress, // raw Pointer into remote process  
      UIntPtr lpParameter,
      uint dwCreationFlags,
      out IntPtr lpThreadId
    );

    [DllImport("kernel32")]
    internal static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr CreateToolhelp32Snapshot([In] uint dwFlags, [In] uint th32ProcessID);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern bool Process32First([In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
    [DllImport("kernel32.dll")]
    internal static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);
    [DllImport("kernel32.dll")]
    internal static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern bool Process32Next([In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtCreateThreadEx(out IntPtr hProcess, AccessMask desiredAccess, IntPtr objectAttributes,
        UIntPtr processHandle, IntPtr startAddress, IntPtr parameter, ThreadCreationFlags inCreateSuspended, int stackZeroBits,
        int sizeOfStack, int maximumStackSize, IntPtr attributeList);


    internal enum NTSTATUS
    {
        Success = 0x00
    }

    internal enum AccessMask
    {
        SpecificRightsAll = 0xFFFF,
        StandardRightsAll = 0x1F0000
    }

    internal enum ThreadCreationFlags
    {
        Immediately = 0x0,
        CreateSuspended = 0x01,
        HideFromDebugger = 0x04,
        StackSizeParamIsAReservation = 0x10000
    }

    internal enum MINIDUMP_TYPE
    {
        MiniDumpNormal = 0x00000000,
        MiniDumpWithDataSegs = 0x00000001,
        MiniDumpWithFullMemory = 0x00000002,
        MiniDumpWithHandleData = 0x00000004,
        MiniDumpFilterMemory = 0x00000008,
        MiniDumpScanMemory = 0x00000010,
        MiniDumpWithUnloadedModules = 0x00000020,
        MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
        MiniDumpFilterModulePaths = 0x00000080,
        MiniDumpWithProcessThreadData = 0x00000100,
        MiniDumpWithPrivateReadWriteMemory = 0x00000200,
        MiniDumpWithoutOptionalData = 0x00000400,
        MiniDumpWithFullMemoryInfo = 0x00000800,
        MiniDumpWithThreadInfo = 0x00001000,
        MiniDumpWithCodeSegs = 0x00002000
    }

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

    internal struct MEMORY_BASIC_INFORMATION32
    {
        public UIntPtr BaseAddress;
        public UIntPtr AllocationBase;
        public uint AllocationProtect;
        public uint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
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

    internal enum SnapshotFlags : uint
    {
        HeapList = 0x00000001,
        Process = 0x00000002,
        Thread = 0x00000004,
        Module = 0x00000008,
        Module32 = 0x00000010,
        Inherit = 0x80000000,
        All = 0x0000001F,
        NoHeaps = 0x40000000
    }

    [Flags]
    internal enum ThreadAccess : int
    {
        TERMINATE = 0x0001,
        SUSPEND_RESUME = 0x0002,
        GET_CONTEXT = 0x0008,
        SET_CONTEXT = 0x0010,
        SET_INFORMATION = 0x0020,
        QUERY_INFORMATION = 0x0040,
        SET_THREAD_TOKEN = 0x0080,
        IMPERSONATE = 0x0100,
        DIRECT_IMPERSONATION = 0x0200
    }

    [Flags]
    internal enum MemoryProtection : uint
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    //inner struct used only internally
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct PROCESSENTRY32
    {
        const int MAX_PATH = 260;
        internal uint dwSize;
        internal uint cntUsage;
        internal uint th32ProcessID;
        internal IntPtr th32DefaultHeapID;
        internal uint th32ModuleID;
        internal uint cntThreads;
        internal uint th32ParentProcessID;
        internal int pcPriClassBase;
        internal uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        internal string szExeFile;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MODULEENTRY32
    {
        internal uint dwSize;
        internal uint th32ModuleID;
        internal uint th32ProcessID;
        internal uint GlblcntUsage;
        internal uint ProccntUsage;
        internal IntPtr modBaseAddr;
        internal uint modBaseSize;
        internal IntPtr hModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string szModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string szExePath;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern int NtQueryInformationThread(
    IntPtr threadHandle,
    ThreadInfoClass threadInformationClass,
    IntPtr threadInformation,
    int threadInformationLength,
    IntPtr returnLengthPtr);
    internal enum ThreadInfoClass : int
    {
        ThreadQuerySetWin32StartAddress = 9
    }
}