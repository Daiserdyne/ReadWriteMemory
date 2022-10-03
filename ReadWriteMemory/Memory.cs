using System.Diagnostics;
using System.Runtime.InteropServices;
using static ReadWriteMemory.NativeImports.NativeMethods;
using ReadWriteMemory.Models;
using ReadWriteMemory.Logging;
using ReadWriteMemory.NativeImports;
using Windows.Services.Maps;
using System;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ReadWriteMemory;

public sealed partial class Memory : IDisposable
{
    #region Fields

    private ProcessInformation? _proc;

    private static readonly object _mem = new();
    private static Memory? _instance;

    private MemoryLogger? _logger;

    private List<MemoryAddressTable> _addressRegister = new();

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

    #region C'tor

    public Memory()
    {
    }

    /// <summary>
    /// Creates an instance of the memory object and opens the target process.
    /// </summary>
    /// <param name="processName"></param>
    public Memory(string processName) =>
        OpenProcess(processName);

    #endregion

    /// <summary>
    /// Open the PC game process with all security and access rights.
    /// </summary>
    /// <returns>Process opened successfully or failed.</returns>
    /// <param name="processName">Show reason open process fails</param>
    public bool OpenProcess(string processName)
    {
        var procId = Process.GetProcessesByName(processName).FirstOrDefault()?.Id;

        int pid = procId ?? 0;

        if (procId <= 0)
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

        if (_proc.Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();

            _proc = null;
            _logger?.Error("Opening process failed.", "opening a handle to the target process failed. Error code: " + error);

            return false;
        }

        _proc.Is64Bit = Environment.Is64BitOperatingSystem
            && IsWow64Process(_proc.Handle, out bool isWow64) && isWow64 is false;

        if (!_proc.Is64Bit)
        {
            _logger?.Error("", ""); // No 32-Bit support.
            return false;
        }

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
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>UIntPtr to created code cave for use for later deallocation</returns>
    public UIntPtr CreateCodeCave(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        if (replaceCount < 5 || _proc is null)
            return UIntPtr.Zero;

        var baseAddress = GetBaseAddress(memAddress);

        var caveAddress = UIntPtr.Zero;

        for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
        {
            caveAddress = VirtualAllocEx(_proc.Handle, FindFreeBlockForRegion(baseAddress, size), size,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            if (caveAddress == UIntPtr.Zero)
                baseAddress = UIntPtr.Add(baseAddress, 0x10000);
        }

        if (caveAddress == UIntPtr.Zero)
            caveAddress = VirtualAllocEx(_proc.Handle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE,
                                         PAGE_EXECUTE_READWRITE);

        int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        // (to - from - 5)
        int offset = (int)((long)caveAddress - (long)baseAddress - 5);

        byte[] jmpBytes = new byte[5 + nopsNeeded];

        jmpBytes[0] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[5 + newBytes.Length];
        offset = (int)((long)baseAddress + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex != -1)
            _addressRegister[tableIndex].CodeCaveTable = new(ReadBytes(baseAddress, replaceCount), caveAddress);

        WriteBytes(caveAddress, caveBytes);
        WriteBytes(baseAddress, jmpBytes);

        return caveAddress;
    }

    /// <summary>
    /// Closes a created code cave with the cave address.
    /// </summary>
    /// <returns></returns>
    public bool CloseCodeCave(MemoryAddress memAddress)
    {
        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
            return false;

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
            return false;

        WriteBytes(baseAddress, caveTable.OriginalOpcode);

        var deallocation = DeallocateMemory(caveTable.CaveAddress);

        if (!deallocation)
            _logger?.Warn("", "Couldn't free memory.");

        _addressRegister[tableIndex].CodeCaveTable = null;

        return deallocation;
    }

    private bool DeallocateMemory(UIntPtr address)
    {
        if (_proc is null)
        {
            _logger?.Error("", "_proc was null and region couldn't be dealloc."); // _proc was null and region couldn't be dealloc.
            return false;
        }

        return VirtualFreeEx(_proc.Handle, address, (UIntPtr)0, 0x8000);
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
                                       (long)tmpAddress % sysInfo.allocationGranularity);

                    // Check if there is enough left
                    if (memoryInfos.RegionSize - offset >= size)
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
                memoryInfos.RegionSize += sysInfo.allocationGranularity - memoryInfos.RegionSize % sysInfo.allocationGranularity;

            previous = current;
            current = new UIntPtr((ulong)memoryInfos.BaseAddress + (ulong)memoryInfos.RegionSize);

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
        if (_proc is null || _proc.Process.Responding is false)
            return;

        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out IntPtr bytesRead);
    }

    public byte[] ReadBytes(UIntPtr address, int length)
    {
        if (_proc is null)
            return new byte[0];

        var opcodes = new byte[length];

        return ReadProcessMemory(_proc.Handle, address, 
            opcodes, (UIntPtr)length, IntPtr.Zero) == true ? opcodes : new byte[0];
    }

    public UIntPtr GetBaseAddress(MemoryAddress memAddress, int addressSize = 16)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        string moduleName = memAddress.ModuleName;

        var baseAddr = GetBaseAddressByMemoryAddress(memAddress);

        if (baseAddr != UIntPtr.Zero)
            return baseAddr;

        var moduleAddress = IntPtr.Zero;

        if (moduleName == string.Empty && int.TryParse(moduleName, out int mAddress))
            moduleAddress = (IntPtr)mAddress;
        else
            moduleAddress = GetModuleAddressByName(moduleName);

        if (moduleAddress == IntPtr.Zero)
        {
            _logger?.Error("Can't get the modules address", "The modules address is IntPtr.Zero.");
            return UIntPtr.Zero;
        }

        var memoryAddress = new byte[addressSize];

        var baseAddress = UIntPtr.Zero;

        int address = memAddress.Address;
        int[]? offsets = memAddress.Offsets;

        if (offsets is not null && offsets.Length != 0)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                    return (UIntPtr)Convert.ToInt64((long)baseAddress + offsets[i]);

                baseAddress = (UIntPtr)Convert.ToInt64((long)baseAddress + offsets[i]);

                ReadProcessMemory(_proc.Handle, baseAddress, memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);

                baseAddress = (UIntPtr)BitConverter.ToInt64(memoryAddress);
            }
        }
        else
        {
            baseAddress = (UIntPtr)IntPtr.Add(moduleAddress, address).ToInt64();
        }

        _addressRegister.Add(new()
        {
            Address = address,
            ModuleName = moduleName,
            Offsets = offsets ?? new int[0],
            BaseAddress = baseAddress,
            UniqueAddressHash = CreateUniqueAddressHash(memAddress)
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

        var baseAddress = _proc.Process.Modules.Cast<ProcessModule>()
            .FirstOrDefault(module => module.ModuleName?.ToLower() == moduleName.ToLower())?.BaseAddress;

        return baseAddress ?? IntPtr.Zero;
    }

    private static string CreateUniqueAddressHash(MemoryAddress memAddres)
    {
        string offsetSequence = string.Empty;

        if (memAddres.Offsets is not null)
            offsetSequence = string.Concat(memAddres.Offsets);

        return memAddres.Address + memAddres.ModuleName + offsetSequence;
    }

    private UIntPtr GetBaseAddressByMemoryAddress(MemoryAddress memAddress)
    {
        string addressHash = CreateUniqueAddressHash(memAddress);

        foreach (var addrTable in _addressRegister)
        {
            if (addrTable.UniqueAddressHash == addressHash)
                return addrTable.BaseAddress;
        }

        return UIntPtr.Zero;
    }

    private int GetAddressIndexByMemoryAddress(MemoryAddress memAddress)
    {
        string addressHash = CreateUniqueAddressHash(memAddress);

        for (int i = 0; i < _addressRegister.Count(); i++)
        {
            if (_addressRegister[i].UniqueAddressHash == addressHash)
                return i;
        }

        return -1;
    }

    public void Dispose()
    {
        CloseProcess();
    }
}