using System.Diagnostics;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Interfaces;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;
using Kernel32 = ReadWriteMemory.External.NativeImports.Kernel32;

namespace ReadWriteMemory.External;

/// <summary>
/// This is the main component of the <see cref="ReadWriteMemory.External"/> library. This class includes a lot of powerful
/// read and write operations to manipulate the memory of a process.
/// </summary>
public partial class RwMemory : IDisposable
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

    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];

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
            if (_targetProcess.Handle == nint.Zero)
            {
                if (OpenProcess())
                {
                    GetAllLoadedProcessModules();
                }
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

    private bool OpenProcess()
    {
        var process = Process.GetProcessesByName(_targetProcess.ProcessName);

        if (process.Length == 0)
        {
            return false;
        }

        var pid = process.First().Id;

        _targetProcess.Process = Process.GetProcessById(pid);

        _targetProcess.Handle = MemoryOperation.OpenProcess(true, pid);

        if (_targetProcess.Handle == nint.Zero)
        {
            ReinitializeTargetProcess();

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
              && Kernel32.IsWow64Process(_targetProcess.Handle, out var isWow64)
              && !isWow64))
        {
            ReinitializeTargetProcess();

            return false;
        }

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

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            _memoryRegister.Add(memoryAddress, new()
            {
                BaseAddress = baseAddress
            });
        }

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
    public void Dispose()
    {
        IDictionary<string, IMemoryTrainer>? implementedTrainer = null;

        try
        {
            implementedTrainer = RwMemoryHelper.GetAllImplementedTrainers();
        }
        catch
        {
            // ignored
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
        StopReadingValuesConstant();
        RestoreAllReplacedBytes();
        CloseHandle();

        _memoryRegister.Clear();
        _monitoringServiceCancellationTokenSrc.Cancel();
        _monitoringServiceCancellationTokenSrc.Dispose();
        OnProcessStateChanged = null;
    }
}