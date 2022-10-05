using System.Diagnostics;
using System.Runtime.InteropServices;
using ReadWriteMemory.Models;
using ReadWriteMemory.Logging;
using ReadWriteMemory.NativeImports;

namespace ReadWriteMemory;

public sealed partial class Memory : NativeMethods, IDisposable
{
    #region Fields

    private ProcessInformation? _proc;

    private static readonly object _mem = new();
    private static Memory? _instance;

    private MemoryLogger? _logger;

    private readonly List<MemoryAddressTable> _addressRegister = new();

    #endregion

    #region Properties

    /// <summary>
    /// Returns a simple logger which allows you to see whats going on here.
    /// </summary>
    public MemoryLogger Logger
    {
        get => _logger ??= new();
    }

    #endregion

    #region C'tor

    /// <summary>
    /// Creates a instance of the memory object without opening the target process.
    /// </summary>
    public Memory()
    {
    }

    /// <summary>
    /// Creates a instance of the memory object and opens the process.
    /// </summary>
    /// <param name="processName"></param>
    public Memory(string processName)
    {
        OpenProcess(processName);
    }

    #endregion

    #region Singelton

    /// <summary>
    /// Creates a singleton instance of the memory object.
    /// </summary>
    /// <returns></returns>
    public static Memory Instance()
    {
        lock (_mem)
        {
            return _instance ??= new();
        }
    }

    /// <summary>
    /// Creates a singleton instance of the memory object and opens the process.
    /// </summary>
    /// <param name="processName"></param>
    /// <returns></returns>
    public static Memory Instance(string processName)
    {
        lock (_mem)
        {
            return _instance ??= new(processName);
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
        var procId = Process.GetProcessesByName(processName).FirstOrDefault()?.Id;

        int pid = procId ?? 0;

        if (procId <= 0)
        {
            _logger?.Error("Opening process failed.", "Can't find this process.");

            return false;
        }

        _proc ??= new();

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

        _ = CloseHandle(_proc.Handle);

        _proc = null;
    }

    /// <summary>
    /// Creates a code cave to write custom opcodes in target process.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Created code cave address</returns>
    public Task<UIntPtr> CreateOrResumeCodeCaveAsync(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        return Task.Run(() => CreateOrResumeCodeCave(memAddress, newBytes, replaceCount, size));
    }

    /// <summary>
    /// Creates a code cave to write custom opcodes in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Created code cave address</returns>
    public UIntPtr CreateOrResumeCodeCave(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        if (replaceCount < 5 || _proc is null)
            return UIntPtr.Zero;

        if (IsCodeCaveOpen(memAddress, out var caveAddr))
            return caveAddr;

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
            _addressRegister[tableIndex].CodeCaveTable =
                new(ReadBytes(baseAddress, replaceCount), caveAddress, jmpBytes);

        WriteBytes(caveAddress, caveBytes);
        WriteBytes(baseAddress, jmpBytes);

        return caveAddress;
    }

    /// <summary>
    /// Checks if a code cave was created in the past with the given memory address.
    /// </summary>
    /// <param name="tableIndex"></param>
    /// <param name="caveAddress"></param>
    /// <returns></returns>
    private bool IsCodeCaveOpen(MemoryAddress memAddress, out UIntPtr caveAddress)
    {
        caveAddress = UIntPtr.Zero;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
            return false;

        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
            return false;

        WriteBytes(_addressRegister[tableIndex].BaseAddress, caveTable.JmpBytes);
        caveAddress = caveTable.CaveAddress;

        return true;
    }

    /// <summary>
    /// Restores the original opcodes to the memory address without dealloacating the memory.
    /// So your code-bytes stay in the memory at the cave address. The advantage is that you
    /// don't have to create a new code cave which costs time. You can simply jump to the cave address
    /// or use the original code. Don't forget to dispose the memory object when you exit the application.
    /// Otherwise the codecaves continue to live forever.
    /// </summary>
    /// <param name="memAddress"></param>
    /// <returns></returns>
    public bool PauseOpenedCodeCave(MemoryAddress memAddress)
    {
        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            _logger?.Error("", $"Couldn't find this memory address: {(IntPtr)memAddress.Address}");
            return false;
        }

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            _logger?.Warn("", "There is currently no opened code cave with this address.");
            return false;
        }

        WriteBytes(baseAddress, caveTable.OriginalOpcode);

        return true;
    }

    /// <summary>
    /// Closes a created code cave. Just give this function the memory address where you create a code cave with.
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

    /// <summary>
    /// Closes all opened code caves by patching the original bytes back and deallocating all allocated memory.
    /// </summary>
    private void CloseAllCodeCaves()
    {
        foreach (var memoryTable in _addressRegister.Where(addr => addr.CodeCaveTable != null))
        {
            var baseAddress = memoryTable.BaseAddress;
            var caveTable = memoryTable.CodeCaveTable;

            if (caveTable is null)
                return;

            WriteBytes(baseAddress, caveTable.OriginalOpcode);

            var deallocation = DeallocateMemory(caveTable.CaveAddress);

            if (!deallocation)
                _logger?.Warn("", "Couldn't free memory.");
        }
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

        GetSystemInfo(out SYSTEM_INFO sysInfo);

        if ((long)minAddress > (long)sysInfo.maximumApplicationAddress ||
            (long)minAddress < (long)sysInfo.minimumApplicationAddress)
            minAddress = sysInfo.minimumApplicationAddress;

        if ((long)maxAddress < (long)sysInfo.minimumApplicationAddress ||
            (long)maxAddress > (long)sysInfo.maximumApplicationAddress)
            maxAddress = sysInfo.maximumApplicationAddress;

        var current = minAddress;
        var caveAddress = UIntPtr.Zero;

        while (VirtualQueryEx(_proc.Handle, current, out MEMORY_BASIC_INFORMATION memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
                return UIntPtr.Zero;  // No memory found, let windows handle

            if (memoryInfos.State == MEM_FREE && memoryInfos.RegionSize > size)
            {
                UIntPtr tmpAddress;

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

                    if ((long)tmpAddress < (long)baseAddress)
                    {
                        tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - size));

                        if ((long)tmpAddress > (long)baseAddress)
                            tmpAddress = baseAddress;

                        tmpAddress =
                            UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                    }

                    if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
                        caveAddress = tmpAddress;
                }
            }

            if (memoryInfos.RegionSize % sysInfo.allocationGranularity > 0)
                memoryInfos.RegionSize += sysInfo.allocationGranularity - memoryInfos.RegionSize % sysInfo.allocationGranularity;

            UIntPtr previous = current;
            current = new UIntPtr((ulong)memoryInfos.BaseAddress + (ulong)memoryInfos.RegionSize);

            if ((long)current >= (long)maxAddress)
                return caveAddress;

            if ((long)previous >= (long)current)
                return caveAddress; // Overflow
        }

        return caveAddress;
    }

    public byte[] ReadBytes(UIntPtr address, int length)
    {
        if (_proc is null)
            return Array.Empty<byte>();

        var bytes = new byte[length];

        return ReadProcessMemory(_proc.Handle, address,
            bytes, (UIntPtr)length, IntPtr.Zero) == true ? bytes : Array.Empty<byte>();
    }

    private UIntPtr GetBaseAddress(MemoryAddress memAddress, int addressSize = 16)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        var savedBaseAddress = GetBaseAddressByMemoryAddress(memAddress);

        if (savedBaseAddress != UIntPtr.Zero)
            return savedBaseAddress;

        IntPtr moduleAddress;

        string moduleName = memAddress.ModuleName;

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

        int address = memAddress.Address;
        int[]? offsets = memAddress.Offsets;

        var baseAddress = (UIntPtr)IntPtr.Add(moduleAddress, address).ToInt64();

        if (offsets is not null && offsets.Length != 0)
        {
            ReadProcessMemory(_proc.Handle, baseAddress, memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);
            baseAddress = (UIntPtr)BitConverter.ToInt64(memoryAddress);

            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    baseAddress = (UIntPtr)Convert.ToInt64((long)baseAddress + offsets[i]);
                    break;
                }

                ReadProcessMemory(_proc.Handle, UIntPtr.Add(baseAddress, offsets[i]), memoryAddress, (UIntPtr)addressSize, IntPtr.Zero);
                baseAddress = (UIntPtr)BitConverter.ToInt64(memoryAddress);
            }
        }

        _addressRegister.Add(new()
        {
            MemoryAddress = memAddress,
            BaseAddress = baseAddress,
            UniqueAddressHash = CreateUniqueAddressHash(memAddress)
        });

        return baseAddress;
    }

    /// <summary>
    /// Gets the process module base address by name.
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

    /// <summary>
    /// Creates an simple and unique address hash.
    /// </summary>
    /// <param name="memAddres"></param>
    /// <returns></returns>
    private static string CreateUniqueAddressHash(MemoryAddress memAddres)
    {
        string offsetSequence = string.Empty;

        if (memAddres.Offsets is not null)
            offsetSequence = string.Concat(memAddres.Offsets);

        return memAddres.Address + memAddres.ModuleName + offsetSequence;
    }

    /// <summary>
    /// Gets the base address from the given memory address object.
    /// </summary>
    /// <param name="memAddress"></param>
    /// <returns>Base address of given memory address object.</returns>
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

    /// <summary>
    /// Searches the given memory address and returns the index in the address register. 
    /// </summary>
    /// <param name="memAddress"></param>
    /// <returns>The index from the given memory address in the addressRegister.</returns>
    private int GetAddressIndexByMemoryAddress(MemoryAddress memAddress)
    {
        string addressHash = CreateUniqueAddressHash(memAddress);

        for (int i = 0; i < _addressRegister.Count; i++)
        {
            if (_addressRegister[i].UniqueAddressHash == addressHash)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Disposes the whole memory object and restores the process normal memory state.
    /// </summary>
    public void Dispose()
    {
        CloseAllCodeCaves();
        CloseProcess();

        _addressRegister.Clear();
        _logger = null;
        _proc = null;
        _instance = null;
    }
}