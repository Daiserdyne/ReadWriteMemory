using System.Collections.Frozen;
using System.Diagnostics;
using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

/// <summary>
/// <inheritdoc cref="ReadWriteMemory"/>
/// </summary>
public partial class RwMemory
{
    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];
    private readonly FrozenDictionary<string, nuint> _modules = GetAllLoadedProcessModules();

    /// <summary>
    /// This is the main component of the <see cref="ReadWriteMemory.Internal"/> library. This class includes a lot of powerfull
    /// read and write operations to manipulate the memory of an process.
    /// </summary>
    public RwMemory()
    {
    }
    
    private static FrozenDictionary<string, nuint> GetAllLoadedProcessModules()
    {
        var modules = new Dictionary<string, nuint>();

        var processModules = Process.GetCurrentProcess()
            .Modules
            .Cast<ProcessModule>();

        foreach (var module in processModules)
        {
            var moduleName = module.ModuleName.ToLower();

            modules.Add(moduleName, (nuint)module.BaseAddress);
        }

        return modules.ToFrozenDictionary();
    }

    /// <summary>
    /// Calculates the final address of the given address with module name and offsets.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    private unsafe nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        var baseAddress = GetBaseAddress(memoryAddress);

        var targetAddress = baseAddress;
        
        if (memoryAddress.Offsets is not null && memoryAddress.Offsets.Any())
        {
            targetAddress = *(nuint*)targetAddress;
            
            for (ushort i = 0; i < memoryAddress.Offsets.Length - 1; i++)
            {
                targetAddress = nuint.Add(targetAddress, memoryAddress.Offsets[i]);
                targetAddress = *(nuint*)targetAddress;
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
            _modules.TryGetValue(moduleName, out moduleAddress);
        }

        var address = memoryAddress.Address;

        if (moduleAddress != nuint.Zero)
        {
            return moduleAddress + address;
        }

        return memoryAddress.Address;
    }
}