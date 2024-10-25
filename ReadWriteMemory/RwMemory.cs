using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;
using System.Diagnostics;
using Kernel32 = ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory;

/// <summary>
/// This is the main component of the <see cref="ReadWriteMemory"/> library. This class includes a lot of powerfull
/// read and write operations to manipulate the memory of an process.
/// </summary>
public sealed partial class RwMemory : IDisposable
{
    /// <summary>
    /// Delegate for the <see cref="RwMemory.ProcessOnStateChanged"/> event.
    /// </summary>
    /// <param name="newProcessState"></param>
    public delegate void ProcessStateHasChanged(ProgramState newProcessState);

    #region Fields

    private readonly HashSet<ProcessStateHasChanged?> _processStateHookCollection = [];
    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];
    private readonly object _lockObjectForProcessState = new();

    private ProcessInformation _targetProcess;

    private ProcessStateHasChanged? _processStateChangedEvent;
    private Task? _isMonitoringServiceRunning;

    /// <summary>
    /// This event will be triggered when the process state changes.
    /// </summary>
    public event ProcessStateHasChanged? ProcessOnStateChanged
    {
        add
        {
            lock (_lockObjectForProcessState)
            {
                _processStateChangedEvent += value;

                if (_processStateHookCollection.Count == 0 ||
                    (_isMonitoringServiceRunning is not null && _isMonitoringServiceRunning.Status != TaskStatus.Running))
                {
                    _isMonitoringServiceRunning = BackgroundService.ExecuteTaskInfinite(StartProcessMonitoringService,
                        TimeSpan.FromMilliseconds(125), _targetProcess.ProcessState.ProcessStateTokenSrc.Token);
                }

                _processStateHookCollection.Add(value);
            }
        }
        remove
        {
            lock (_lockObjectForProcessState)
            {
                if (!_processStateHookCollection.Remove(value))
                {
                    _targetProcess.ProcessState.ProcessStateTokenSrc.Cancel();
                    _isMonitoringServiceRunning = null;

                    return;
                }

                _processStateChangedEvent -= value;

                if (!_processStateHookCollection.Any())
                {
                    _targetProcess.ProcessState.ProcessStateTokenSrc.Cancel();
                    _isMonitoringServiceRunning = null;
                }
            }
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns the current state of the process. 
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsProcessAlive => _targetProcess.ProcessState.IsProcessAlive;

    #endregion

    #region C'tor

    /// <summary>
    /// This is the main component of the <see cref="ReadWriteMemory"/> library. This class includes a lot of powerfull
    /// read and write operations to manipulate the memory of an process.
    /// </summary>
    public RwMemory(string processName)
    {
        _targetProcess = new ProcessInformation
        {
            ProcessName = processName
        };
    }

    #endregion

    private void StartProcessMonitoringService()
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
                else
                {
                    // todo: Add logic when process can't be opened.
                }
            }

            _targetProcess.ProcessState.IsProcessAlive = true;

            TriggerStateChangedEvent(oldProcessState);

            return;
        }

        _targetProcess.ProcessState.IsProcessAlive = false;

        if (_targetProcess.Handle != nint.Zero)
        {
            _targetProcess = new ProcessInformation
            {
                ProcessName = _targetProcess.ProcessName
            };

            _memoryRegister.Clear();
        }

        TriggerStateChangedEvent(oldProcessState);
    }

    private void TriggerStateChangedEvent(bool oldProcessState)
    {
        if (oldProcessState != IsProcessAlive)
        {
            _processStateChangedEvent?.Invoke(IsProcessAlive ? ProgramState.Running : ProgramState.NotRunning);
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

        _targetProcess = new ProcessInformation
        {
            ProcessName = _targetProcess.ProcessName
        };

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
            .Select(addr => addr.FreezeTokenSrc))
        {
            freezeTokenSrc!.Cancel();
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
            _targetProcess = new ProcessInformation
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && Kernel32.IsWow64Process(_targetProcess.Handle, out var isWow64)
            && isWow64 is false))
        {
            _targetProcess = new ProcessInformation
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        var mainModule = _targetProcess.Process.MainModule;

        if (mainModule is null)
        {
            _targetProcess = new ProcessInformation
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        return true;
    }

    private nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        var baseAddress = GetBaseAddress(memoryAddress);

        var targetAddress = baseAddress;

        if (memoryAddress.Offsets is not null && memoryAddress.Offsets.Any())
        {
            var buffer = new byte[nint.Size];

            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer);

            MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);

            for (ushort i = 0; i < memoryAddress.Offsets.Length; i++)
            {
                if (i == memoryAddress.Offsets.Length - 1)
                {
                    targetAddress = nuint.Add(targetAddress, memoryAddress.Offsets[i]);

                    break;
                }

                MemoryOperation.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, memoryAddress.Offsets[i]), buffer);

                MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);
            }
        }

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable
            {
                BaseAddress = baseAddress
            });
        }

        return targetAddress;
    }

    private nuint GetBaseAddress(MemoryAddress memoryAddress)
    {
        if (_memoryRegister.TryGetValue(memoryAddress, out var value) && value.BaseAddress != nuint.Zero)
        {
            return _memoryRegister[memoryAddress].BaseAddress;
        }

        var moduleAddress = nuint.Zero;

        var moduleName = memoryAddress.ModuleName;

        if (!string.IsNullOrEmpty(moduleName) 
            && _targetProcess.Modules.TryGetValue(moduleName, out var module))
        {
            moduleAddress = module;
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
        CloseHandle();

        _memoryRegister.Clear();
        _targetProcess.ProcessState.ProcessStateTokenSrc.Cancel();
        _processStateChangedEvent = null;
    }
}