using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using ReadWriteMemory.NativeImports;
using ReadWriteMemory.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReadWriteMemory;

/// <summary>
/// This is the main component of the <see cref="ReadWriteMemory"/> package. This class includes a lot of powerfull
/// read and write operations to manipulate the memory of an process.
/// </summary>
public sealed partial class Memory : NativeMethods, IDisposable
{
    #region Constants

    private const short Vector3Length = 3;

    #endregion

    #region Fields

    /// <summary>
    /// Delegate for the <see cref="Process_OnStateChanged"/> event.
    /// </summary>
    /// <param name="newProcessState"></param>
    public delegate void ProcessStateHasChanged(bool newProcessState);

    /// <summary>
    /// This event will be triggered when the process state changes.
    /// </summary>
    public event ProcessStateHasChanged? Process_OnStateChanged;

    private ProcessInformation _proc;

    private MemoryLogger? _logger;

    private readonly ProcessState _procState = new();
    private readonly List<MemoryAddressTable> _addressRegister = new();
    private readonly byte[] _buffer = new byte[8];

    #endregion

    #region Properties

    /// <summary>
    /// Gives you a logger instance which allows you to see whats going on here.
    /// </summary>
    public MemoryLogger Logger
    {
        get => _logger ??= new();
    }

    #endregion

    #region C'tor

    /// <summary>
    /// Creates a instance of the memory object.
    /// </summary>
    /// <param name="processName"></param>
    public Memory(string processName)
    {
        _proc = new()
        {
            ProcessName = processName
        };

        StartProcessStateService();
    }

    #endregion

    private void StartProcessStateService()
    {
        bool oldProcessState = _procState.CurrentProcessState;

        _ = BackgroundService.ExecuteTaskInfiniteAsync(() =>
        {
            if (Process.GetProcessesByName(_proc.ProcessName).Any())
            {
                _procState.CurrentProcessState = true;

                if (_proc.Handle == IntPtr.Zero)
                    OpenProcess();

                TriggerStateChangedEvent(ref oldProcessState);

                return;
            }

            _procState.CurrentProcessState = false;

            if (_proc.Handle != IntPtr.Zero)
            {
                _proc = new()
                {
                    ProcessName = _proc.ProcessName
                };

                _addressRegister.Clear();

                _logger?.Warn($"Target process \"{_proc.ProcessName}\" isn't running anymore.");
            }

            TriggerStateChangedEvent(ref oldProcessState);
        }, TimeSpan.FromMilliseconds(250), _procState.ProcessStateTokenSrc.Token);
    }

    private void TriggerStateChangedEvent(ref bool oldState)
    {
        if (oldState != _procState.CurrentProcessState)
        {
            Process_OnStateChanged?.Invoke(_procState.CurrentProcessState);
            oldState = _procState.CurrentProcessState;
        }
    }

    private bool OpenProcess()
    {
        int? tmpPid = Process.GetProcessesByName(_proc.ProcessName)[0]?.Id;

        if (tmpPid is null)
            return false;

        int pid = (int)tmpPid;

        _proc.Process = Process.GetProcessById(pid);
        _proc.Handle = OpenProcess(true, pid);

        if (_proc.Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();

            _logger?.Error($"Opening process failed. Process handle was {IntPtr.Zero}. Error code: {error}");

            _proc = new()
            {
                ProcessName = _proc.ProcessName
            };

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && IsWow64Process(_proc.Handle, out bool isWow64)
            && isWow64 is false))
        {
            _logger?.Error("Target process or operation system are not 64 bit.\n" +
                "This library only supports 64-bit processes and os.");

            _proc = new()
            {
                ProcessName = _proc.ProcessName
            };

            return false;
        }

        var mainModule = _proc.Process?.MainModule;

        if (mainModule is null)
        {
            _logger?.Error("Couldn't get main module from target process.");

            _proc = new()
            {
                ProcessName = _proc.ProcessName
            };

            return false;
        }

        _proc.MainModule = mainModule;

        _logger?.Info($"Target process \"{_proc.ProcessName}\" opened successfully.");

        return true;
    }

    /// <summary>
    /// Closes the process when finished.
    /// </summary>
    public void CloseProcess()
    {
        if (!IsProcessAlive() || _proc.Handle == IntPtr.Zero)
            return;

        _ = CloseHandle(_proc.Handle);

        _proc = new()
        {
            ProcessName = _proc.ProcessName
        };

        _addressRegister.Clear();
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
        if (replaceCount < 5 || !IsProcessAlive())
            return UIntPtr.Zero;

        if (IsCodeCaveOpen(memAddress, out var caveAddr))
        {
            _logger?.Info($"Resuming code cave for address 0x{(UIntPtr)memAddress.Address:x16}.\nCave address: 0x{caveAddr:x16}\n");
            return caveAddr;
        }

        var targetAddress = GetTargetAddress(memAddress);

        var caveAddress = UIntPtr.Zero;

        for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
        {
            caveAddress = VirtualAllocEx(_proc.Handle, FindFreeBlockForRegion(targetAddress, size), size,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            if (caveAddress == UIntPtr.Zero)
                targetAddress = UIntPtr.Add(targetAddress, 0x10000);
        }

        if (caveAddress == UIntPtr.Zero)
            caveAddress = VirtualAllocEx(_proc.Handle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE,
                                         PAGE_EXECUTE_READWRITE);

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

        _logger?.Info($"Code cave created for address 0x{memAddress.Address:x16}.\nCustom code at cave address: " +
            $"0x{caveAddress:x16}. Opcodes patched.\n");

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
        if (!IsProcessAlive())
            return false;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            _logger?.Info($"Can't pause code cave because there is currently no opened cave.");
            return false;
        }

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            _logger?.Info($"Can't pause code cave because there is currently no opened cave.");
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
        if (!IsProcessAlive())
            return false;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            _logger?.Info($"Can't close code cave because there is currently no opened cave.");
            return false;
        }

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            _logger?.Info($"Can't close code cave because there is currently no opened cave.");
            return false;
        }

        WriteBytes(baseAddress, caveTable.OriginalOpcodes);

        var deallocation = DeallocateMemory(caveTable.CaveAddress);

        if (!deallocation)
            _logger?.Info($"Couldn't free allocated code cave at address: {caveTable.CaveAddress:x16}");

        _addressRegister[tableIndex].CodeCaveTable = null;

        return deallocation;
    }

    /// <summary>
    /// Disposes the whole memory object and restores the process normal memory state.
    /// </summary>
    public void Dispose()
    {
        foreach (var trainer in TrainerServices.GetAllImplementedTrainers().Values
            .Where(x => x.DisableWhenDispose)) trainer.Disable();

        CloseAllCodeCaves();
        UnfreezeAllValues();
        CloseProcess();

        _addressRegister.Clear();
        _procState.ProcessStateTokenSrc.Cancel();
        Process_OnStateChanged = null;
        _logger = null;
    }
}