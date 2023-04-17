using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Trainer.Interface;
using ReadWriteMemory.Utilities;
using System.Diagnostics;

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

    private readonly List<MemoryAddressTable> _addressRegister = new();
    private readonly byte[] _buffer = new byte[8];

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

    private void GetAllProcessModules()
    {
        _targetProcess.Modules = new Dictionary<string, ProcessModule>();

        foreach (var module in _targetProcess.Process.Modules.Cast<ProcessModule>())
        {
            _targetProcess.Modules.Add(module.ModuleName.ToLower(), module);
        }
    }

    private void StartProcessStateMonitorService(ref bool oldProcessState)
    {
        if (Process.GetProcessesByName(_targetProcess.ProcessName).Any())
        {
            _targetProcess.ProcessState.CurrentProcessState = true;

            if (_targetProcess.Handle == IntPtr.Zero)
            {
                if (OpenProcess())
                {
                    GetAllProcessModules();
                }
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

            _targetProcess.Modules = null;

            _addressRegister.Clear();
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

    /// <summary>
    /// Closes the process when finished.
    /// </summary>
    public void CloseProcess()
    {
        if (!IsProcessAlive() || _targetProcess.Handle == IntPtr.Zero)
        {
            return;
        }

        NativeImports.Win32.CloseHandle(_targetProcess.Handle);

        _targetProcess = new()
        {
            ProcessName = _targetProcess.ProcessName
        };

        _targetProcess.Modules = null;

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
    public Task<nuint> CreateOrResumeCodeCaveAsync(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        return Task.Run(() => CreateOrResumeCodeCave(memAddress, newBytes, replaceCount, size));
    }

    /// <summary>
    /// Creates a code cave to apply custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newCode">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Cave address</returns>
    public nuint CreateOrResumeCodeCave(MemoryAddress memAddress, byte[] newCode, int replaceCount, uint size = 0x1000)
    {
        if (replaceCount < 5 || !IsProcessAlive())
        {
            return nuint.Zero;
        }

        if (IsCodeCaveAlreadyCreatedForAddress(memAddress, out var caveAddr))
        {
            return caveAddr;
        }

        var targetAddress = GetTargetAddress(memAddress);

        CodeCaveFactory.CreateCodeCaveAndInjectCode(targetAddress, _targetProcess.Handle, newCode, replaceCount,
            out var caveAddress, out var originalOpcodes, out var jmpBytes, size);

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex != -1)
        {
            _addressRegister[tableIndex].CodeCaveTable = new(originalOpcodes, caveAddress, jmpBytes);
        }

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
        {
            return false;
        }

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            return false;
        }

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, baseAddress, caveTable.OriginalOpcodes);

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
            return false;
        }

        var baseAddress = _addressRegister[tableIndex].BaseAddress;
        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, baseAddress, caveTable.OriginalOpcodes);

        _addressRegister[tableIndex].CodeCaveTable = null;

        var deallocation = DeallocateMemory(caveTable.CaveAddress);

        return deallocation;
    }

    private bool IsProcessAlive()
    {
        if (_targetProcess.ProcessState.CurrentProcessState == false)
        {
            return false;
        }

        return _targetProcess.ProcessState.CurrentProcessState;
    }

    private bool DeallocateMemory(nuint address)
    {
        if (!IsProcessAlive())
        {
            return false;
        }

        return MemoryOperation.DeallocateMemory(_targetProcess.Handle, address);
    }

    /// <summary>
    /// Checks if a code cave was created in the past with the given memory address.
    /// </summary>
    /// <param name="memAddress"></param>
    /// <param name="caveAddress"></param>
    /// <returns></returns>
    private bool IsCodeCaveAlreadyCreatedForAddress(MemoryAddress memAddress, out nuint caveAddress)
    {
        caveAddress = nuint.Zero;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            return false;
        }

        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, _addressRegister[tableIndex].BaseAddress, caveTable.JmpBytes);

        caveAddress = caveTable.CaveAddress;

        return true;
    }

    /// <summary>
    /// Closes all opened code caves by patching the original bytes back and deallocating all allocated memory.
    /// </summary>
    private void CloseAllCodeCaves()
    {
        foreach (var memoryTable in _addressRegister
            .Where(addr => addr.CodeCaveTable is not null))
        {
            var baseAddress = memoryTable.BaseAddress;
            var caveTable = memoryTable.CodeCaveTable;

            if (caveTable is null)
            {
                return;
            }

            MemoryOperation.WriteProcessMemory(_targetProcess.Handle, baseAddress, caveTable.OriginalOpcodes);

            DeallocateMemory(caveTable.CaveAddress);
        }
    }

    private void UnfreezeAllValues()
    {
        foreach (var freezeTokenSrc in _addressRegister
            .Where(addr => addr.FreezeTokenSrc is not null)
            .Select(addr => addr.FreezeTokenSrc))
        {
            freezeTokenSrc!.Cancel();
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
        _targetProcess.Handle = NativeImports.Win32.OpenProcess(true, pid);

        if (_targetProcess.Handle == IntPtr.Zero)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            _targetProcess.Modules = null;

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && NativeImports.Win32.IsWow64Process(_targetProcess.Handle, out bool isWow64)
            && isWow64 is false))
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            _targetProcess.Modules = null;

            return false;
        }

        var mainModule = _targetProcess.Process?.MainModule;

        if (mainModule is null)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            _targetProcess.Modules = null;

            return false;
        }

        _targetProcess.MainModule = mainModule;

        return true;
    }

    /// <summary>
    /// Disposes the whole memory object and restores the process normal memory state.
    /// </summary>
    public void Dispose()
    {
        IDictionary<string, ITrainer>? implementedTrainer = null;

        try
        {
            implementedTrainer = TrainerServices.GetAllImplementedTrainers();
        }
        catch
        {
            // Ex in logger.
        }

        if (implementedTrainer is not null)
        {
            foreach (var trainer in implementedTrainer.Values
                .Where(x => x.DisableWhenDispose))
            {
                trainer.Disable();
            }
        }

        CloseAllCodeCaves();
        UnfreezeAllValues();
        CloseProcess();

        _addressRegister.Clear();
        _targetProcess.ProcessState.ProcessStateTokenSrc.Cancel();
        Process_OnStateChanged = null;
    }
}