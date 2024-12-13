using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

/// <summary>
/// <inheritdoc cref="ReadWriteMemory"/>
/// </summary>
public partial class RwMemory
{
    private readonly Dictionary<MemoryAddress, MemoryAddressTable> _memoryRegister = [];
    private readonly Dictionary<string, nuint> _modules = [];

    /// <summary>
    /// This is the main component of the <see cref="ReadWriteMemory.Internal"/> library. This class includes a lot of powerfull
    /// read and write operations to manipulate the memory of an process.
    /// </summary>
    public RwMemory()
    {
    }

    private bool TryGetModuleHandle(string moduleName, out nuint moduleHandle)
    {
        if (_modules.TryGetValue(moduleName, out moduleHandle))
        {
            return true;
        }
        
        var handle = Kernel32.GetModuleHandle(moduleName);

        if (handle == nint.Zero)
        {
            moduleHandle = nuint.Zero;
            return false;
        }
        
        _modules.Add(moduleName, (nuint)handle);

        return true;
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

        if (memoryAddress.Offsets.Any())
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
            return address + moduleAddress;
        }

        return memoryAddress.Address;
    }
}