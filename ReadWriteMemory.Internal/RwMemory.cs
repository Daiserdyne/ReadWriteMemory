using System.Diagnostics;
using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

/// <summary>
/// <inheritdoc cref="ReadWriteMemory"/>
/// </summary>
public partial class RwMemory
{
    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];
    private readonly Dictionary<string, nuint> _modules = [];
    
    private void GetAllLoadedProcessModules()
    {
        var processModules = Process.GetCurrentProcess().Modules
            .Cast<ProcessModule>()
            .ToList();

        foreach (var module in processModules)
        {
            var moduleName = module.ModuleName.ToLower();

            if (!_modules.ContainsKey(moduleName))
            {
                _modules.Add(moduleName, (nuint)module.BaseAddress);
                continue;
            }

            _modules[moduleName] = (nuint)module.BaseAddress;
        }
    }

    private unsafe nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        var baseAddress = GetBaseAddress(memoryAddress);

        var targetAddress = baseAddress;
        
        if (memoryAddress.Offsets is not null && memoryAddress.Offsets.Any())
        {
            for (uint i = 0; i < memoryAddress.Offsets.Length - 1; i++)
            {
                targetAddress = nuint.Add(targetAddress, memoryAddress.Offsets[i]);
                targetAddress = *(nuint*)targetAddress;
            }
            
            targetAddress = nuint.Add(targetAddress, 
                memoryAddress.Offsets[memoryAddress.Offsets.Length - 1]);
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
    
    private unsafe nuint GetBaseAddress(MemoryAddress memoryAddress)
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
            _modules.TryGetValue(moduleName, out moduleAddress);
        }

        var address = memoryAddress.Address;

        if (moduleAddress != nuint.Zero)
        {
            return *(nuint*)(moduleAddress + address);
        }

        return *(nuint*)memoryAddress.Address;
    }
}