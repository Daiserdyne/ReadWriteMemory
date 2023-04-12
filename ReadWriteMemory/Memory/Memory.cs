using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory;

/// <summary>
/// This is the main component of the <see cref="ReadWriteMemory"/> package. This class includes a lot of powerfull
/// read and write operations to manipulate the memory of an process.
/// </summary>
public sealed partial class Memory : IDisposable
{
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

    private ProcessInformation _targetProcess;
    private MemoryLogger? _logger;

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
        _targetProcess = new()
        {
            ProcessName = processName
        };

        // ProcessState in ProcessInformation einbauen.
        var oldProcessState = _targetProcess.ProcessState.CurrentProcessState;

        _ = BackgroundService.ExecuteTaskInfinite(() => StartProcessStateMonitorService(ref oldProcessState),
            TimeSpan.FromMilliseconds(250), _targetProcess.ProcessState.ProcessStateTokenSrc.Token);
    }

    #endregion

    private void StartProcessStateMonitorService(ref bool oldProcessState)
    {
        if (Process.GetProcessesByName(_targetProcess.ProcessName).Any())
        {
            _targetProcess.ProcessState.CurrentProcessState = true;

            if (_targetProcess.Handle == IntPtr.Zero)
            {
                OpenProcess();
            }

            TriggerStateChangedEvent(ref oldProcessState);

            return;
        }

        _targetProcess.ProcessState.CurrentProcessState = false;

        if (_targetProcess.Handle != IntPtr.Zero)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            _addressRegister.Clear();

            _logger?.Warn($"Target process \"{_targetProcess.ProcessName}\" isn't running anymore.");
        }

        TriggerStateChangedEvent(ref oldProcessState);
    }

    private void TriggerStateChangedEvent(ref bool oldState)
    {
        if (oldState != _targetProcess.ProcessState.CurrentProcessState)
        {
            Process_OnStateChanged?.Invoke(_targetProcess.ProcessState.CurrentProcessState);
            oldState = _targetProcess.ProcessState.CurrentProcessState;
        }
    }

    private bool OpenProcess()
    {
        var process = Process.GetProcessesByName(_targetProcess.ProcessName);

        if (process is null || !process.Any())
        {
            return false;
        }

        var pid = process.First().Id;

        _targetProcess.Process = Process.GetProcessById(pid);
        _targetProcess.Handle = Win32.OpenProcess(true, pid);

        if (_targetProcess.Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();

            _logger?.Error($"Getting handle to access process memory failed. Error code: {error}");

            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && Win32.IsWow64Process(_targetProcess.Handle, out bool isWow64)
            && isWow64 is false))
        {
            _logger?.Error("Target process or operation system are not 64 bit.\n" +
                "This library only supports 64-bit processes and os.");

            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        var mainModule = _targetProcess.Process?.MainModule;

        if (mainModule is null)
        {
            _logger?.Error("Couldn't get main module from target process.");

            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        _targetProcess.MainModule = mainModule;

        _logger?.Info($"Attaching to targetprocess \"{_targetProcess.ProcessName}\" was successfully.");

        return true;
    }

    /// <summary>
    /// Closes the process when finished.
    /// </summary>
    public void CloseProcess()
    {
        if (!IsProcessAlive() || _targetProcess.Handle == IntPtr.Zero)
        {
            return;
        }

        Win32.CloseHandle(_targetProcess.Handle);

        _logger?.Info($"Detaching from targetprocess \"{_targetProcess.ProcessName}\" was successfully.");

        _targetProcess = new()
        {
            ProcessName = _targetProcess.ProcessName
        };

        _addressRegister.Clear();
    }

    /// <summary>
    /// Creates a code cave to inject custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Code cave address</returns>
    public Task<UIntPtr> CreateOrResumeCodeCaveAsync(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        return Task.Run(() => CreateOrResumeCodeCave(memAddress, newBytes, replaceCount, size));
    }

    /// <summary>
    /// Creates a code cave to inject custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Cave address</returns>
    public UIntPtr CreateOrResumeCodeCave(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        if (replaceCount < 5 || !IsProcessAlive())
        {
            return UIntPtr.Zero;
        }

        if (IsCodeCaveOpen(memAddress, out var caveAddr))
        {
            _logger?.Info($"Resuming code cave for address 0x{(UIntPtr)memAddress.Address:x16}.\nCave address: 0x{caveAddr:x16}\n");
            return caveAddr;
        }

        var targetAddress = GetTargetAddress(memAddress);

        var codeCaveTable = CodeCave.CreateCodeCaveAndInjectCode(targetAddress, _targetProcess.Handle, newBytes, replaceCount, size);

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex != -1)
        {
            _addressRegister[tableIndex].CodeCaveTable = codeCaveTable;
        }

        return codeCaveTable.CaveAddress;
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
        {
            return false;
        }

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

        _logger?.Info($"Code cave for target address: 0x{baseAddress:x16} was paused. Allocaded memory remains. Cave address is: 0x{caveTable.CaveAddress:x16}");

        return true;
    }

    /// <summary>
    /// Closes a created code cave. Just give this function the memory address where you create a code cave with.
    /// </summary>
    /// <returns></returns>
    public bool CloseCodeCave(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
        {
            return false;
        }

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
        {
            _logger?.Info($"Couldn't free allocated code cave at address: {caveTable.CaveAddress:x16}");
        }

        _addressRegister[tableIndex].CodeCaveTable = null;

        return deallocation;
    }

    /// <summary>
    /// Disposes the whole memory object and restores the process normal memory state.
    /// </summary>
    public void Dispose()
    {
        foreach (var trainer in TrainerServices.GetAllImplementedTrainers().Values
            .Where(x => x.DisableWhenDispose))
        {
            trainer.Disable();
        }

        CloseAllCodeCaves();
        UnfreezeAllValues();
        CloseProcess();

        _addressRegister.Clear();
        _targetProcess.ProcessState.ProcessStateTokenSrc.Cancel();
        Process_OnStateChanged = null;
        _logger = null;
    }
}