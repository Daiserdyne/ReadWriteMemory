using Memory.Imports;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Memory.Models;
using Memory.Logging;
using static Memory.Imports.NativeMethods;
using System.Globalization;
using Memory.Structures;

namespace Memory;

public sealed class Memory
{
    #region Fields

    private ProcessInformation? _proc;

    private static readonly object _mem = new();
    private static Memory? _instance;

    private MemoryLogger? _logger;

    private List<MemoryAddress<UIntPtr>> _x64AddressRegister = new();
    private List<MemoryAddress<IntPtr>> _x86AddressRegister = new();

    #endregion

    #region Properties

    /// <summary>
    /// Returns a singelton Instance of the memory object.
    /// </summary>
    public static Memory Instance
    {
        get
        {
            lock (_mem)
            {
                if (_instance is null)
                    _instance = new();

                return _instance;
            }
        }
    }

    /// <summary>
    /// Returns a simple logger which allows you to see whats going on here.
    /// </summary>
    public MemoryLogger Logger
    {
        get
        {
            if (_logger is null)
                _logger = new();

            return _logger;
        }
    }

    #endregion

    /// <summary>
    /// Open the PC game process with all security and access rights.
    /// </summary>
    /// <returns>Process opened successfully or failed.</returns>
    /// <param name="processName">Show reason open process fails</param>
    public bool OpenProcess(string processName)
    {
        var pid = Process.GetProcessesByName(processName).First().Id;

        if (pid <= 0)
        {
            _logger?.Error("Opening process failed.", "Can't find this process.");

            return false;
        }

        if (_proc is null)
            _proc = new();

        _proc.ProcessName = processName;
        _proc.Process = Process.GetProcessById(pid);

        if (_proc.Process is not null && _proc.Process.Responding is false)
        {
            _proc = null;
            _logger?.Error("Opening process failed.", "Process is not responding or null.");

            return false;
        }

        _proc.Handle = NativeMethods.OpenProcess(true, pid);

        try
        {
            Process.EnterDebugMode();
        }
        catch (Exception)
        {
            _logger?.Warn("Opening process failed.", "Couldn't enter debug mode.");
        }

        if (_proc.Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();

            Process.LeaveDebugMode();

            _proc = null;
            _logger?.Error("Opening process failed.", "opening a handle to the target process failed. Error code: " + error);

            return false;
        }

        _proc.Is64Bit = Environment.Is64BitOperatingSystem
            && IsWow64Process(_proc.Handle, out bool isWow64) && isWow64 is false;

        var mainModule = _proc.Process?.MainModule;

        if (mainModule is null)
        {
            _logger?.Error("Opening process failed.", "Main module was null.");

            return false;
        }

        _proc.MainModule = mainModule;

        return true;
    }

    /// <summary>
    /// Closes the process when finished.
    /// </summary>
    public void CloseProcess()
    {
        if (_proc is null || _proc.Handle == IntPtr.Zero)
            return;

        CloseHandle(_proc.Handle);

        _proc = null;
    }

    /// <summary>
    /// Creates a code cave to write custom opcodes in target process
    /// </summary>
    /// <param name="code">Address to create the trampoline</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <param name="file">ini file to look in</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>UIntPtr to created code cave for use for later deallocation</returns>
    public UIntPtr CreateCodeCave(string code, byte[] newBytes, int replaceCount, int size = 0x1000, string file = "")
    {
        if (replaceCount < 5 || _proc is null)
            return UIntPtr.Zero; // returning UIntPtr.Zero instead of throwing an exception
                                 // to better match existing code

        //var theCode = GetCode(code, file);
        var theCode = UIntPtr.Zero;

        UIntPtr address = theCode;

        // if x64 we need to try to allocate near the address so we dont run into the +-2GB limit of the 0xE9 jmp

        UIntPtr caveAddress = UIntPtr.Zero;
        UIntPtr prefered = address;

        for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
        {
            caveAddress = VirtualAllocEx(_proc.Handle, FindFreeBlockForRegion(prefered, (uint)size), (uint)size,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            if (caveAddress == UIntPtr.Zero)
                prefered = UIntPtr.Add(prefered, 0x10000);
        }

        // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
        if (caveAddress == UIntPtr.Zero)
            caveAddress = VirtualAllocEx(_proc.Handle, UIntPtr.Zero, (uint)size, MEM_COMMIT | MEM_RESERVE,
                                         PAGE_EXECUTE_READWRITE);

        int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        // (to - from - 5)
        int offset = (int)((long)caveAddress - (long)address - 5);

        byte[] jmpBytes = new byte[5 + nopsNeeded];

        jmpBytes[0] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[5 + newBytes.Length];
        offset = (int)((long)address + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

        WriteBytes(caveAddress, caveBytes);
        WriteBytes(address, jmpBytes);

        return caveAddress;
    }

    private UIntPtr FindFreeBlockForRegion(UIntPtr baseAddress, uint size)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        var minAddress = UIntPtr.Subtract(baseAddress, 0x70000000);
        var maxAddress = UIntPtr.Add(baseAddress, 0x70000000);

        var caveAddress = UIntPtr.Zero;

        GetSystemInfo(out SYSTEM_INFO sysInfo);

        if (_proc.Is64Bit)
        {
            if ((long)minAddress > (long)sysInfo.maximumApplicationAddress ||
                (long)minAddress < (long)sysInfo.minimumApplicationAddress)
                minAddress = sysInfo.minimumApplicationAddress;

            if ((long)maxAddress < (long)sysInfo.minimumApplicationAddress ||
                (long)maxAddress > (long)sysInfo.maximumApplicationAddress)
                maxAddress = sysInfo.maximumApplicationAddress;
        }
        else
        {
            minAddress = sysInfo.minimumApplicationAddress;
            maxAddress = sysInfo.maximumApplicationAddress;
        }

        var current = minAddress;
        var previous = current;

        MEMORY_BASIC_INFORMATION memoryInfos;

        var tmpAddress = UIntPtr.Zero;

        while (VirtualQueryEx(_proc.Handle, current, out memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
                return UIntPtr.Zero;  // No memory found, let windows handle

            if (memoryInfos.State == MEM_FREE && memoryInfos.RegionSize > size)
            {
                if ((long)memoryInfos.BaseAddress % sysInfo.allocationGranularity > 0)
                {
                    // The whole size can not be used
                    tmpAddress = memoryInfos.BaseAddress;
                    int offset = (int)(sysInfo.allocationGranularity -
                                       ((long)tmpAddress % sysInfo.allocationGranularity));

                    // Check if there is enough left
                    if ((memoryInfos.RegionSize - offset) >= size)
                    {
                        // yup there is enough
                        tmpAddress = UIntPtr.Add(tmpAddress, offset);

                        if ((long)tmpAddress < (long)baseAddress)
                        {
                            tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - offset - size));

                            if ((long)tmpAddress > (long)baseAddress)
                                tmpAddress = baseAddress;

                            // decrease tmpAddress until its alligned properly
                            tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                        }

                        // if the difference is closer then use that
                        if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
                            caveAddress = tmpAddress;
                    }
                }
                else
                {
                    tmpAddress = memoryInfos.BaseAddress;

                    if ((long)tmpAddress < (long)baseAddress) // try to get it the cloest possible 
                                                              // (so to the end of the region - size and
                                                              // aligned by system allocation granularity)
                    {
                        tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - size));

                        if ((long)tmpAddress > (long)baseAddress)
                            tmpAddress = baseAddress;

                        // decrease until aligned properly
                        tmpAddress =
                            UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                    }

                    if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
                        caveAddress = tmpAddress;
                }
            }

            if (memoryInfos.RegionSize % sysInfo.allocationGranularity > 0)
                memoryInfos.RegionSize += sysInfo.allocationGranularity - (memoryInfos.RegionSize % sysInfo.allocationGranularity);

            previous = current;
            current = new UIntPtr(((ulong)memoryInfos.BaseAddress) + (ulong)memoryInfos.RegionSize);

            if ((long)current >= (long)maxAddress)
                return caveAddress;

            if ((long)previous >= (long)current)
                return caveAddress; // Overflow
        }

        return caveAddress;
    }

    private UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer)
    {
        UIntPtr retVal;

        if (_proc is null || _proc.Is64Bit || IntPtr.Size == 8)
        {
            // 64 bit
            MEMORY_BASIC_INFORMATION64 tmp64 = new();
            retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64, new UIntPtr((uint)Marshal.SizeOf(tmp64)));

            lpBuffer.BaseAddress = tmp64.BaseAddress;
            lpBuffer.AllocationBase = tmp64.AllocationBase;
            lpBuffer.AllocationProtect = tmp64.AllocationProtect;
            lpBuffer.RegionSize = (long)tmp64.RegionSize;
            lpBuffer.State = tmp64.State;
            lpBuffer.Protect = tmp64.Protect;
            lpBuffer.Type = tmp64.Type;

            return retVal;
        }

        MEMORY_BASIC_INFORMATION32 tmp32 = new();

        retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp32, new UIntPtr((uint)Marshal.SizeOf(tmp32)));

        lpBuffer.BaseAddress = tmp32.BaseAddress;
        lpBuffer.AllocationBase = tmp32.AllocationBase;
        lpBuffer.AllocationProtect = tmp32.AllocationProtect;
        lpBuffer.RegionSize = tmp32.RegionSize;
        lpBuffer.State = tmp32.State;
        lpBuffer.Protect = tmp32.Protect;
        lpBuffer.Type = tmp32.Type;

        return retVal;
    }

    /// <summary>
    /// Write byte array to address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    public void WriteBytes(UIntPtr address, byte[] write)
    {
        if (_proc is null)
            return;

        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out IntPtr bytesRead);
    }

    /// <summary>
    /// Calculates the true address.
    /// </summary>
    /// <param name="size">size of address (default is 8)</param>
    /// <param name="address">size of address (default is 8)</param>
    /// <returns></returns>
    public UIntPtr GetTargetAddress(string modulename, int address, params int[]? offests)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        if (_proc.Is64Bit)
            return Get64BitAddress(address, modulename, offests);

        return (UIntPtr)Convert.ToInt64(Get32BitAddress(address, modulename));
    }

    public UIntPtr Get64BitAddress(int address, string moduleName = "", params int[]? offests)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        foreach (var x64Address in _x64AddressRegister)
        {
            if (x64Address.UniqueAddressHash == CreateUniqueAddressHash(address, moduleName, offests))
                return x64Address.BaseAddress;
        }

        var moduleAddress = IntPtr.Zero;

        if (moduleName != "" && int.TryParse(moduleName, out int mAddress))
            moduleAddress = (IntPtr)mAddress;
        else
            moduleAddress = GetModuleAddressByName(moduleName);

        if (moduleAddress == IntPtr.Zero)
        {
            _logger?.Error("Can't get the modules address", "The modules address is IntPtr.Zero.");
            return UIntPtr.Zero;
        }

        int addressSize = 16;

        var memoryAddress = new byte[addressSize];

        ReadProcessMemory(_proc.Handle, (UIntPtr)((long)moduleAddress + (long)address),
                        memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);

        var baseAddress = (UIntPtr)BitConverter.ToInt64(memoryAddress, 0);

        if (offests is not null)
        {
            foreach (var offset in offests)
            {   // This shit dont work
                baseAddress = (UIntPtr)Convert.ToInt64((long)baseAddress + offset);
                ReadProcessMemory(_proc.Handle, baseAddress, memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);
                baseAddress = (UIntPtr)BitConverter.ToInt64(memoryAddress, 0);

            }
        }

        _x64AddressRegister.Add(new()
        {
            Address = address,
            ModuleName = moduleName,
            Offsets = offests ?? new int[0],
            BaseAddress = baseAddress,
            UniqueAddressHash = CreateUniqueAddressHash(address, moduleName, offests)
        });

        return baseAddress;
    }

    public IntPtr Get32BitAddress(int address, string moduleName = "", params int[]? offests)
    {
        if (_proc is null)
            return IntPtr.Zero;

        foreach (var x86Address in _x86AddressRegister)
        {
            if (x86Address.UniqueAddressHash == CreateUniqueAddressHash(address, moduleName, offests))
                return x86Address.BaseAddress;
        }

        var moduleAddress = IntPtr.Zero;

        if (moduleName != "" && int.TryParse(moduleName, out int mAddress))
            moduleAddress = (IntPtr)mAddress;
        else
            moduleAddress = GetModuleAddressByName(moduleName);

        if (moduleAddress == IntPtr.Zero)
        {
            _logger?.Error("Can't get the modules address", "The modules address is IntPtr.Zero.");
            return IntPtr.Zero;
        }

        int addressSize = 8;

        var memoryAddress = new byte[addressSize];

        ReadProcessMemory(_proc.Handle, (UIntPtr)((int)moduleAddress + address),
                        memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);

        var baseAddress = (IntPtr)BitConverter.ToInt32(memoryAddress);

        if (offests is not null)
        {
            foreach (var offset in offests)
            {
                ReadProcessMemory(_proc.Handle, (UIntPtr)((int)baseAddress + offset),
                    memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);

                baseAddress = (IntPtr)BitConverter.ToInt32(memoryAddress);
            }
        }

        _x86AddressRegister.Add(new()
        {
            Address = address,
            ModuleName = moduleName,
            Offsets = offests ?? new int[0],
            BaseAddress = baseAddress,
            UniqueAddressHash = CreateUniqueAddressHash(address, moduleName, offests)
        });

        return baseAddress;
    }

    /// <summary>
    /// Retrieve _proc. Process module baseaddress by name
    /// </summary>
    /// <param name="moduleName">name of module</param>
    /// <returns></returns>
    private IntPtr GetModuleAddressByName(string moduleName)
    {
        if (_proc is null)
        {
            _logger?.Error("Couldn't get module address by name", $"{nameof(_proc)} was null.");
            return IntPtr.Zero;
        }

        return _proc.Process.Modules.Cast<ProcessModule>()
            .First(module => module.ModuleName == moduleName).BaseAddress;
    }

    private static string CreateUniqueAddressHash(int address, string moduleName, int[]? offests)
    {
        string offsetSequence = string.Empty;

        if (offests is not null)
            offsetSequence = string.Concat(offests);

        return address + moduleName + offsetSequence;
    }
}