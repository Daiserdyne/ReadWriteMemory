using System.Diagnostics;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;
using ReadWriteMemory.Shared.Entities;
using ReadWriteMemory.Shared.Interfaces;
using ReadWriteMemory.Shared.Services;
using Kernel32 = ReadWriteMemory.External.NativeImports.Kernel32;

namespace ReadWriteMemory.External;

/// <summary>
/// This is the main component of the <see cref="ReadWriteMemory.External"/> library. This class includes a lot of powerfull
/// read and write operations to manipulate the memory of an process.
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
    public delegate void ReinitilizeTargetProcess();

    /// <summary>
    /// This event will be triggered when the process state changes.
    /// </summary>
    public event ProcessStateHasChanged? OnProcessStateChanged;

    /// <summary>
    /// This will be triggered when the whole internal attributes get reinitialized.
    /// </summary>
    public event ReinitilizeTargetProcess? OnReinitilizeTargetProcess;

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
    /// This is the main component of the <see cref="ReadWriteMemory.External"/> library. This class includes a lot of powerfull
    /// read and write operations to manipulate the memory of an process.
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

        OnReinitilizeTargetProcess?.Invoke();
    }

    #endregion

    /// <summary>
    /// This service updates the current state of the program. It also triggers the program
    /// state changed event.
    /// </summary>
    private void ProcessMonitoringService()
    {
        var oldProcessState = IsProcessAlive;

        if (Process.GetProcessesByName(_targetProcess.ProcessName).Any())
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
        if (!IsProcessAlive)
        {
            return false;
        }

        return MemoryOperation.DeallocateMemory(_targetProcess.Handle, address);
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

    private bool OpenProcess()
    {
        var process = Process.GetProcessesByName(_targetProcess.ProcessName);

        if (!process.Any())
        {
            return false;
        }

        var pid = process.First().Id;

        _targetProcess.Process = Process.GetProcessById(pid);

        _targetProcess.Handle = Kernel32.OpenProcess(true, pid);

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

        if (mainModule is null)
        {
            ReinitializeTargetProcess();

            return false;
        }

        return true;
    }

    private nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        var baseAddress = GetBaseAddress(memoryAddress);

        var targetAddress = baseAddress;

        if (memoryAddress.Offsets.Any())
        {
            var buffer = new byte[nuint.Size];

            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer);

            MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);

            for (ushort i = 0; i < memoryAddress.Offsets.Length - 1; i++)
            {
                MemoryOperation.ReadProcessMemory(_targetProcess.Handle,
                    nuint.Add(targetAddress, memoryAddress.Offsets[i]), buffer);

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
            targetAddress = default;

            return false;
        }

        targetAddress = GetTargetAddress(memoryAddress);

        return true;
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