using System.Diagnostics;
using System.Runtime.InteropServices;
using ReadWriteMemory.Models;
using ReadWriteMemory.Logging;
using ReadWriteMemory.NativeImports;
using ReadWriteMemory.Services;

namespace ReadWriteMemory;

public sealed partial class Memory : NativeMethods, IDisposable
{
    #region Constants

    private const int Size = 16;

    #endregion

    #region Fields

    private ProcessInformation? _proc;

    private static readonly object _mem = new();
    private static Memory? _instance;

    private MemoryLogger? _logger;

    private readonly List<MemoryAddressTable> _addressRegister = new();

    //private string _targetProcessName = string.Empty;
    //private CancellationTokenSource? _checkProcessStatusSrc;

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
    /// Creates a instance of the memory object and opens the process.
    /// </summary>
    /// <param name="processName"></param>
    public Memory(string processName)
    {
        OpenProcess(processName);

        BackgroundService.ExecuteTaskAsync(() =>
        {
            if (Process.GetProcessesByName(processName).Length == 0 || _proc is null)
            {
                if (_proc is not null)
                    _proc = null;

                if (_addressRegister.Count != 0)
                    _addressRegister.Clear();

                OpenProcess(processName);
            }
        }, TimeSpan.FromMilliseconds(500), new CancellationTokenSource().Token);

    }

    #endregion

    #region Singelton

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
    private bool OpenProcess(string processName)
    {
        //_targetProcessName = processName;

        var procId = Process.GetProcessesByName(processName).FirstOrDefault()?.Id;

        int pid = procId ?? 0;

        if (procId is null)
        {
            _logger?.Error("ERROR", "Target process isn't running.");

            return false;
        }

        _proc ??= new();
        _proc.ProcessName = processName;
        _proc.Process = Process.GetProcessById(pid);

        if (!IsProcessAliveAndResponding())
            return false;

        _proc.Handle = OpenProcess(true, pid);

        if (_proc.Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();

            _logger?.Error("ERROR", $"Opening process failed. Process handle was {IntPtr.Zero}. Error code: {error}");

            _proc = null;

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && IsWow64Process(_proc.Handle, out bool isWow64)
            && isWow64 is false))
        {
            _logger?.Error("ERROR", "Target process or operation system are not 64 bit.\n" +
                "This library only supports 64-bit processes and os.");

            _proc = null;

            return false;
        }

        var mainModule = _proc.Process?.MainModule;

        if (mainModule is null)
        {
            _logger?.Error("ERROR", "Couldn't get main module from target process.");

            _proc = null;

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
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (!IsProcessAliveAndResponding() || _proc.Handle == IntPtr.Zero)
            return;
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

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
    public Task<UIntPtr> CreateOrResumeCodeCaveAsync(MemoryAddress memAddress, byte[] newBytes,
        int replaceCount, uint size = 0x1000)
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
    public UIntPtr CreateOrResumeCodeCave(MemoryAddress memAddress, byte[] newBytes, int replaceCount,
        uint size = 0x1000)
    {
        if (replaceCount < 5 || !IsProcessAliveAndResponding())
            return UIntPtr.Zero;

        if (IsCodeCaveOpen(memAddress, out var caveAddr))
            return caveAddr;

        var targetAddress = GetTargetAddress(memAddress);

        var caveAddress = UIntPtr.Zero;

        for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
        {
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
            caveAddress = VirtualAllocEx(_proc.Handle, FindFreeBlockForRegion(targetAddress, size), size,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

            if (caveAddress == UIntPtr.Zero)
                targetAddress = UIntPtr.Add(targetAddress, 0x10000);
        }

        if (caveAddress == UIntPtr.Zero)
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
            caveAddress = VirtualAllocEx(_proc.Handle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE,
                                         PAGE_EXECUTE_READWRITE);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        // (to - from - 5)
        int offset = (int)((long)caveAddress - (long)targetAddress - 5);

        byte[] jmpBytes = new byte[5 + nopsNeeded];

        jmpBytes[0] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[5 + newBytes.Length];
        offset = (int)((long)targetAddress + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex != -1)
            _addressRegister[tableIndex].CodeCaveTable =
                new(ReadBytes(targetAddress, replaceCount), caveAddress, jmpBytes);

        WriteBytes(caveAddress, caveBytes);
        WriteBytes(targetAddress, jmpBytes);

        return caveAddress;
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

        WriteBytes(baseAddress, caveTable.OriginalOpcodes);

        return true;
    }

    /// <summary>
    /// Closes a created code cave. Just give this function the memory address where you create a code cave with.
    /// </summary>
    /// <returns></returns>
    public bool CloseCodeCave(MemoryAddress memAddress)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
            return false;

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
            return false;

        WriteBytes(baseAddress, caveTable.OriginalOpcodes);

        var deallocation = DeallocateMemory(caveTable.CaveAddress);

        if (!deallocation)
            _logger?.Warn("", "Couldn't free memory.");

        _addressRegister[tableIndex].CodeCaveTable = null;

        return deallocation;
    }

    /// <summary>
    /// Disposes the whole memory object and restores the process normal memory state.
    /// </summary>
    public void Dispose()
    {
        CloseAllCodeCaves();
        UnfreezeAllValues();
        CloseProcess();

        _addressRegister.Clear();
        _logger = null;
        _proc = null;
        _instance = null;
    }
}