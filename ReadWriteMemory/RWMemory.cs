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
public sealed partial class RWMemory : IDisposable
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

    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];

    #endregion

    #region Properties

    private bool IsProcessAlive => _targetProcess.ProcessState.IsProcessAlive;

    #endregion

    #region C'tor

    /// <summary>
    /// This is the main component of the <see cref="ReadWriteMemory"/> library. This class includes a lot of powerfull
    /// read and write operations to manipulate the memory of an process.
    /// </summary>
    public RWMemory(string processName)
    {
        _targetProcess = new()
        {
            ProcessName = processName
        };

        var oldProcessState = _targetProcess.ProcessState.IsProcessAlive;

        _ = BackgroundService.ExecuteTaskInfinite(() => StartProcessMonitoringService(ref oldProcessState),
            TimeSpan.FromMilliseconds(150), _targetProcess.ProcessState.ProcessStateTokenSrc.Token);
    }

    #endregion

    private void StartProcessMonitoringService(ref bool oldProcessState)
    {
        if (Process.GetProcessesByName(_targetProcess.ProcessName).Any())
        {
            if (_targetProcess.Handle == nint.Zero)
            {
                if (OpenProcess())
                {
                    GetAllLoadedProcessModules();
                }
            }

            _targetProcess.ProcessState.IsProcessAlive = true;

            TriggerStateChangedEvent(ref oldProcessState);

            return;
        }

        _targetProcess.ProcessState.IsProcessAlive = false;

        if (_targetProcess.Handle != nint.Zero)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            _memoryRegister.Clear();
        }

        TriggerStateChangedEvent(ref oldProcessState);
    }

    private void GetAllLoadedProcessModules()
    {
        var processModules = _targetProcess.Process.Modules.Cast<ProcessModule>();

        foreach (var module in processModules)
        {
            if (!_targetProcess.Modules.ContainsKey(module.ModuleName.ToLower()))
            {
                _targetProcess.Modules.Add(module.ModuleName.ToLower(), (nuint)module.BaseAddress);
            }
        }
    }

    private void TriggerStateChangedEvent(ref bool oldState)
    {
        if (oldState != _targetProcess.ProcessState.IsProcessAlive)
        {
            Process_OnStateChanged?.Invoke(_targetProcess.ProcessState.IsProcessAlive);
            oldState = _targetProcess.ProcessState.IsProcessAlive;
        }
    }

    /// <summary>
    /// Closes the process when finished.
    /// </summary>
    public void CloseHandle()
    {
        if (!IsProcessAlive || _targetProcess.Handle == nint.Zero)
        {
            return;
        }

        _ = Kernel32.CloseHandle(_targetProcess.Handle);

        _targetProcess = new()
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

        if (process is null || !process.Any())
        {
            return false;
        }

        var pid = process.First().Id;

        _targetProcess.Process = Process.GetProcessById(pid);
        _targetProcess.Handle = Kernel32.OpenProcess(true, pid);

        if (_targetProcess.Handle == nint.Zero)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && Kernel32.IsWow64Process(_targetProcess.Handle, out bool isWow64)
            && isWow64 is false))
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        var mainModule = _targetProcess.Process?.MainModule;

        if (mainModule is null)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        _targetProcess.MainModule = mainModule;

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
            _memoryRegister.Add(memoryAddress, new()
            {
                MemoryAddress = memoryAddress,
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

        if (!string.IsNullOrEmpty(moduleName) && _targetProcess.Modules.ContainsKey(moduleName))
        {
            moduleAddress = _targetProcess.Modules[moduleName];
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
        Process_OnStateChanged = null;
    }
}