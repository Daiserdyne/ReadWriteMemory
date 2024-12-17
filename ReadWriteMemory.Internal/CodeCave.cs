using ReadWriteMemory.Internal.Entities;
using static ReadWriteMemory.Internal.NativeImports.Kernel32;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="customCode"></param>
    /// <param name="amountOfOpcodesToReplace"></param>
    /// <param name="totalAmountOfOpcodesToReplace"></param>
    /// <param name="memoryToAllocate"></param>
    /// <returns></returns>
    public async ValueTask<nuint> CreateOrResumeCodeCave(MemoryAddress memoryAddress, byte[] customCode,
        uint amountOfOpcodesToReplace, uint totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (table.CodeCaveTable is not null)
        {
            return table.CodeCaveTable.Value.CaveAddress;
        }
        
        return await Task.Run(() => CreateCodeCave(memoryAddress, customCode, amountOfOpcodesToReplace, 
            totalAmountOfOpcodesToReplace, memoryToAllocate));
    }

    private nuint CreateCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> customCode,
        uint amountOfOpcodesToReplace, uint totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        nuint targetAddress;
        
        try
        {
            targetAddress = GetTargetAddress(memoryAddress);
        }
        catch 
        {
            return nuint.Zero;
        }

        if (!ReadBytes(memoryAddress, amountOfOpcodesToReplace, out var readBytes))
        {
            return nuint.Zero;
        }
        
        var caveAddress = VirtualAlloc(nint.Zero, memoryToAllocate, 
            AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);

        if (caveAddress == nint.Zero)
        {
            return nuint.Zero;
        }
        
        // todo: continue implementing the rest of the function.
        
        return nuint.Zero;
    }
}