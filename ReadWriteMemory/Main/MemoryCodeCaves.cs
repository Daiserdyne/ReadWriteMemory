using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    /// <summary>
    /// Creates a code cave to inject custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Code cave address</returns>
    public Task<nuint> CreateOrResumeCodeCaveAsync(MemoryAddress memAddress, byte[] newBytes, int replaceCount, uint size = 0x1000)
    {
        return Task.Run(() => CreateOrResumeCodeCave(memAddress, newBytes, replaceCount, size));
    }

    /// <summary>
    /// Creates a code cave to apply custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memoryAddress">Address, module name and offesets</param>
    /// <param name="newCode">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Cave address</returns>
    public nuint CreateOrResumeCodeCave(MemoryAddress memoryAddress, byte[] newCode, int replaceCount, uint size = 0x1000)
    {
        if (replaceCount < 5 || !IsProcessAlive)
        {
            return nuint.Zero;
        }

        if (IsCodeCaveAlreadyCreatedForAddress(memoryAddress, out var caveAddr))
        {
            return caveAddr;
        }

        var targetAddress = GetTargetAddress(memoryAddress);

        CodeCaveFactory.CreateCodeCaveAndInjectCode(targetAddress, _targetProcess.Handle, newCode, replaceCount,
            out var caveAddress, out var originalOpcodes, out var jmpBytes, size);

        if (_memoryRegister.ContainsKey(memoryAddress))
        {
            _memoryRegister[memoryAddress].CodeCaveTable = new(originalOpcodes, caveAddress, jmpBytes);
        }

        return caveAddress;
    }

    /// <summary>
    /// Checks if a code cave was created in the past with the given memory address.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="caveAddress"></param>
    /// <returns></returns>
    private bool IsCodeCaveAlreadyCreatedForAddress(MemoryAddress memoryAddress, out nuint caveAddress)
    {
        caveAddress = nuint.Zero;

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            return false;
        }

        var caveTable = _memoryRegister[memoryAddress].CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, _memoryRegister[memoryAddress].BaseAddress, caveTable.JmpBytes))
        {
            return false;
        }

        caveAddress = caveTable.CaveAddress;

        return true;
    }
}