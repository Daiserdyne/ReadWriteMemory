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

        CodeCaveFactory2.CreateCodeCaveAndInjectCode(targetAddress, _targetProcess.Handle, newCode, replaceCount,
            out var caveAddress, out var originalOpcodes, out var jmpBytes, size);

        _memoryRegister[memoryAddress].CodeCaveTable = new(originalOpcodes, caveAddress, jmpBytes);

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

        if (!_memoryRegister.TryGetValue(memoryAddress, out var memoryTable))
        {
            return false;
        }

        var caveTable = memoryTable.CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        if (caveTable.CaveAddress != nuint.Zero)
        {
            caveAddress = caveTable.CaveAddress;

            if (!MemoryOperation.WriteProcessMemory(_targetProcess.Handle, memoryTable.BaseAddress, caveTable.JmpBytes))
            {
                MemoryOperation.WriteProcessMemory(_targetProcess.Handle, memoryTable.BaseAddress, caveTable.OriginalOpcodes);

                DeallocateMemory(caveTable.CaveAddress);

                return false;
            }

            return true;
        }

        return false;
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

        if (!_memoryRegister.TryGetValue(memoryAddress, out var memoryTable))
        {
            return false;
        }

        var baseAddress = memoryTable.BaseAddress;
        var caveTable = memoryTable.CodeCaveTable;

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

        if (!_memoryRegister.TryGetValue(memoryAddress, out var memoryTable))
        {
            return false;
        }

        var baseAddress = memoryTable.BaseAddress;
        var caveTable = memoryTable.CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, baseAddress, caveTable.OriginalOpcodes);

        memoryTable.CodeCaveTable = null;

        return DeallocateMemory(caveTable.CaveAddress);
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
}