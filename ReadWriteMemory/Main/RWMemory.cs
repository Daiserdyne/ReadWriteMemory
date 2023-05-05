using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;
using System.Diagnostics;

namespace ReadWriteMemory.Main;

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

    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = new();

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
            if (_targetProcess.Handle == IntPtr.Zero)
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

        if (_targetProcess.Handle != IntPtr.Zero)
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
        foreach (var module in _targetProcess.Process.Modules.Cast<ProcessModule>())
        {
            if (!_targetProcess.Modules.ContainsKey(module.ModuleName.ToLower()))
            {
                _targetProcess.Modules.Add(module.ModuleName.ToLower(), module.BaseAddress);
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
        if (!IsProcessAlive || _targetProcess.Handle == IntPtr.Zero)
        {
            return;
        }

        _ = NativeImports.Kernel32.CloseHandle(_targetProcess.Handle);

        _targetProcess = new()
        {
            ProcessName = _targetProcess.ProcessName
        };

        _memoryRegister.Clear();
    }

    /// <summary>
    /// Restores the original opcodes to the memory address without dealloacating the memory.
    /// So your code-bytes stay in the memory at the cave address. The advantage is that you
    /// don't have to create a new code cave which costs time. You can simply jump to the cave address
    /// or use the original code. Don't forget to dispose the memory object when you exit the application.
    /// Otherwise the codecaves continue to live forever.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool PauseOpenedCodeCave(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive)
        {
            return false;
        }

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            return false;
        }

        var baseAddress = _memoryRegister[memoryAddress].BaseAddress;
        var caveTable = _memoryRegister[memoryAddress].CodeCaveTable;

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
    public bool CloseCodeCave(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive)
        {
            return false;
        }

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            return false;
        }

        var baseAddress = _memoryRegister[memoryAddress].BaseAddress;
        var caveTable = _memoryRegister[memoryAddress].CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, baseAddress, caveTable.OriginalOpcodes);

        _memoryRegister[memoryAddress].CodeCaveTable = null;

        return DeallocateMemory(caveTable.CaveAddress);
    }

    private bool DeallocateMemory(nuint address)
    {
        if (!IsProcessAlive)
        {
            return false;
        }

        return MemoryOperation.DeallocateMemory(_targetProcess.Handle, address);
    }

    /// <summary>
    /// Closes all opened code caves by patching the original bytes back and deallocating all allocated memory.
    /// </summary>
    private void CloseAllCodeCaves()
    {
        foreach (var memoryTable in _memoryRegister.Values
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
        _targetProcess.Handle = NativeImports.Kernel32.OpenProcess(true, pid);

        if (_targetProcess.Handle == IntPtr.Zero)
        {
            _targetProcess = new()
            {
                ProcessName = _targetProcess.ProcessName
            };

            return false;
        }

        if (!(Environment.Is64BitOperatingSystem
            && NativeImports.Kernel32.IsWow64Process(_targetProcess.Handle, out bool isWow64)
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

    private unsafe nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        nuint baseAddress = default;

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            return baseAddress;
        }

        var savedBaseAddress = _memoryRegister[memoryAddress].BaseAddress;

        if (savedBaseAddress != nuint.Zero)
        {
            baseAddress = savedBaseAddress;
        }
        else
        {
            var moduleAddress = IntPtr.Zero;

            var moduleName = memoryAddress.ModuleName;

            if (!string.IsNullOrEmpty(moduleName) && _targetProcess.Modules.ContainsKey(moduleName))
            {
                moduleAddress = _targetProcess.Modules[moduleName];
            }

            var address = memoryAddress.Address;

            if (moduleAddress != IntPtr.Zero)
            {
                baseAddress = (nuint)(moduleAddress + address);
            }
            else
            {
                baseAddress = (nuint)memoryAddress.Address;
            }
        }

        var targetAddress = baseAddress;

        if (memoryAddress.Offsets is not null && memoryAddress.Offsets.Any())
        {
            var buffer = new byte[nint.Size];

            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer);

            MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);

            for (ushort index = 0; index < memoryAddress.Offsets.Length; index++)
            {
                if (index == memoryAddress.Offsets.Length - 1)
                {
                    targetAddress = (nuint)Convert.ToUInt64((long)targetAddress + memoryAddress.Offsets[index]);

                    break;
                }

                MemoryOperation.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, memoryAddress.Offsets[index]), buffer);

                MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);
            }
        }

        _memoryRegister.Add(memoryAddress, new()
        {
            MemoryAddress = memoryAddress,
            BaseAddress = baseAddress
        });

        return targetAddress;
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