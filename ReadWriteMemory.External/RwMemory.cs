using System.Collections.Concurrent;
using System.Diagnostics;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;
using Kernel32 = ReadWriteMemory.External.NativeImports.Kernel32;

namespace ReadWriteMemory.External;

/// <summary>
/// This is the main component of the <see cref="ReadWriteMemory.External"/> library. This class includes a lot of powerful
/// read and write operations to manipulate the memory of a process.
/// </summary>
public sealed partial class RwMemory : IAsyncDisposable
{
    #region Events and Delegates

    /// <summary>
    /// Delegate for the <see cref="ProcessStateHasChanged"/> event.
    /// </summary>
    /// <param name="newProcessState"></param>
    public delegate void ProcessStateHasChanged(ProgramState newProcessState);

    /// <summary>
    /// 
    /// </summary>
    public delegate void ReInitializeTargetProcess();

    /// <summary>
    /// This event will be triggered when the process state changes.
    /// </summary>
    public event ProcessStateHasChanged? OnProcessStateChanged;

    /// <summary>
    /// This will be triggered when the whole internal attributes get reinitialized.
    /// </summary>
    public event ReInitializeTargetProcess? OnReInitializeTargetProcess;

    #endregion

    #region Fields

    private readonly ConcurrentDictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];

    private readonly CancellationTokenSource _monitoringServiceCancellationTokenSrc = new();

    private ProcessInformation _targetProcess;

    #endregion

    #region Properties

    /// <summary>
    /// Returns the current state of the process. 
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsProcessAlive => _targetProcess.IsProcessAlive;

    #endregion

    #region C'tor

    /// <summary>
    /// This is the main component of the <see cref="ReadWriteMemory.External"/> library. This class includes a
    /// lot of powerful read and write operations to manipulate the memory of a process.
    /// </summary>
    public RwMemory(string processName)
    {
        _targetProcess = new()
        {
            ProcessName = processName
        };

        _ = BackgroundService.ExecuteTaskRepeatedly(ProcessMonitoringService,
            TimeSpan.FromMilliseconds(125), _monitoringServiceCancellationTokenSrc.Token);
    }

    private void ReinitializeTargetProcess()
    {
        _targetProcess = new()
        {
            ProcessName = _targetProcess.ProcessName
        };

        OnReInitializeTargetProcess?.Invoke();
    }

    #endregion

    /// <summary>
    /// This service updates the current state of the program. It also triggers the program
    /// state changed event.
    /// </summary>
    private void ProcessMonitoringService()
    {
        var oldProcessState = IsProcessAlive;

        if (Process.GetProcessesByName(_targetProcess.ProcessName).Length != 0)
        {
            if (_targetProcess.Handle == nint.Zero && OpenProcess())
            {
                GetAllLoadedProcessModules();
            }

            _targetProcess.IsProcessAlive = true;

            TriggerStateChangedEvent(oldProcessState);

            return;
        }

        _targetProcess.IsProcessAlive = false;

        if (_targetProcess.Handle != nint.Zero)
        {
            ReinitializeTargetProcess();

            _memoryRegister.Clear();
        }

        TriggerStateChangedEvent(oldProcessState);
    }

    private void TriggerStateChangedEvent(bool oldProcessState)
    {
        if (oldProcessState != IsProcessAlive)
        {
            OnProcessStateChanged?.Invoke(IsProcessAlive ? ProgramState.Started : ProgramState.Closed);
        }
    }

    private void GetAllLoadedProcessModules()
    {
        var processModules = _targetProcess.Process.Modules
            .Cast<ProcessModule>()
            .ToList();

        foreach (var module in processModules)
        {
            var moduleName = module.ModuleName.ToLower();

            if (!_targetProcess.Modules.ContainsKey(moduleName))
            {
                _targetProcess.Modules.Add(moduleName, (nuint)module.BaseAddress);
                continue;
            }

            _targetProcess.Modules[moduleName] = (nuint)module.BaseAddress;
        }
    }

    /// <summary>
    /// Closes the process when finished.
    /// </summary>
    private void CloseHandle()
    {
        if (!IsProcessAlive || _targetProcess.Handle == nint.Zero)
        {
            return;
        }

        _ = Kernel32.CloseHandle(_targetProcess.Handle);

        ReinitializeTargetProcess();

        _memoryRegister.Clear();
    }

    private bool DeallocateMemory(nuint address)
    {
        return IsProcessAlive && MemoryOperation.DeallocateMemory(_targetProcess.Handle, address);
    }

    private void UnfreezeAllValues()
    {
        foreach (var freezeTokenSrc in _memoryRegister.Values
                     .Where(addr => addr.FreezeTokenSrc is not null)
                     .Select(addr => addr.FreezeTokenSrc!))
        {
            freezeTokenSrc.Cancel();
            freezeTokenSrc.Dispose();
        }
    }

    private void StopReadingValuesConstant()
    {
        foreach (var readValueConstantTokenSrc in _memoryRegister.Values
                     .Where(addr => addr.ReadValueConstantTokenSrc is not null)
                     .Select(addr => addr.ReadValueConstantTokenSrc!))
        {
            readValueConstantTokenSrc.Cancel();
            readValueConstantTokenSrc.Dispose();
        }
    }

    private void RestoreAllReplacedBytes()
    {
        foreach (var (memoryAddress, table) in _memoryRegister)
        {
            if (table.ReplacedBytes is not null)
            {
                UndoReplaceBytes(memoryAddress);
            }
        }
    }

    private void CloseAllCodeCaves()
    {
        foreach (var memoryTable in _memoryRegister.Values
                     .Where(addr => addr.CodeCaveTable is not null))
        {
            var baseAddress = memoryTable.BaseAddress;
            var caveTable = memoryTable.CodeCaveTable;

            if (caveTable is null)
            {
                continue;
            }

            MemoryOperation.WriteProcessMemory(_targetProcess.Handle, baseAddress, caveTable.Value.OriginalOpcodes);

            _ = DeallocateMemory(caveTable.Value.CaveAddress);
        }
    }

    private bool OpenProcess()
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            throw new Exception("This library requires a 64-bit operating system.");
        }
        
        var process = Process.GetProcessesByName(_targetProcess.ProcessName);

        if (process.Length == 0)
        {
            return false;
        }

        var pid = process.FirstOrDefault()?.Id;

        if (pid is null)
        {
            throw new NullReferenceException("Process not found or pid was null");
        }
        
        _targetProcess.Handle = MemoryOperation.OpenProcess(true, pid.Value);

        if (Kernel32.IsWow64Process2(_targetProcess.Handle, out _, 
                out var isProcessIs64Bit))
        {
            if (isProcessIs64Bit != Kernel32.Amd64Code)
            {
                throw new Exception("This library does only support x64 games, not x86 games. Sorry :(.");
            }

            if (_targetProcess.Handle == nint.Zero)
            {
                throw new NullReferenceException("Could not get a valid handle of the process. " +
                                                 "Maybe try to run it with Administrator rights.");
            }
        }
        else
        {
            throw new Exception("Could not call the WinApi function 'IsWow64Process2' successfully. " +
                                "Maybe try to run it with Administrator rights.");
        }
        
        _targetProcess.Process = Process.GetProcessById(pid.Value);

        var mainModule = _targetProcess.Process.MainModule;

        if (mainModule is not null)
        {
            return true;
        }

        ReinitializeTargetProcess();

        return false;
    }

    private nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        var baseAddress = GetBaseAddress(memoryAddress);

        var targetAddress = baseAddress;

        if (memoryAddress.Offsets.Length != 0)
        {
            var buffer = new byte[nuint.Size];

            if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                return nuint.Zero;
            }

            MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);

            for (ushort i = 0; i < memoryAddress.Offsets.Length - 1; i++)
            {
                if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle,
                        nuint.Add(targetAddress, memoryAddress.Offsets[i]), buffer))
                {
                    return nuint.Zero;
                }

                MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);
            }

            targetAddress = nuint.Add(targetAddress, memoryAddress.Offsets[^1]);
        }

        _memoryRegister.GetOrAdd(memoryAddress, static (_, address) =>
            new() { BaseAddress = address }, baseAddress);

        return targetAddress;
    }

    private nuint GetBaseAddress(MemoryAddress memoryAddress)
    {
        if (_memoryRegister.TryGetValue(memoryAddress, out var value)
            && value.BaseAddress != nuint.Zero)
        {
            return _memoryRegister[memoryAddress].BaseAddress;
        }

        var moduleAddress = nuint.Zero;

        var moduleName = memoryAddress.ModuleName;

        if (!string.IsNullOrEmpty(moduleName))
        {
            _targetProcess.Modules.TryGetValue(moduleName, out moduleAddress);
        }

        var address = memoryAddress.Address;

        if (moduleAddress != nuint.Zero)
        {
            return moduleAddress + address;
        }

        return memoryAddress.Address;
    }

    private bool GetTargetAddress(MemoryAddress memoryAddress, out nuint targetAddress)
    {
        if (!IsProcessAlive)
        {
            targetAddress = 0;

            return false;
        }

        targetAddress = GetTargetAddress(memoryAddress);

        return targetAddress != nuint.Zero;
    }

    /// <summary>
    /// Disposes the whole memory object and restores the process normal memory state.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // The try is in case it's native aot compiled.
        try
        {
            var implementedTrainer = RwMemoryHelper.GetAllImplementedTrainers();
            
            if (implementedTrainer.Count > 0)
            {
                foreach (var trainer in implementedTrainer.Values
                             .Where(x => x.DisableWhenDispose))
                {
                    await trainer.Disable();
                }
            }
        }
        catch
        {
            // ignored
        }
       
        CloseAllCodeCaves();
        UnfreezeAllValues();
        StopReadingValuesConstant();
        RestoreAllReplacedBytes();
        CloseHandle();

        _memoryRegister.Clear();
        
        await _monitoringServiceCancellationTokenSrc.CancelAsync();
        
        _monitoringServiceCancellationTokenSrc.Dispose();
        OnProcessStateChanged = null;
    }
}