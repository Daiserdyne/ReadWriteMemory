using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using ReadWriteMemory.Utilities.CodeCave;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    /// <summary>
    /// Creates a code cave to apply custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memoryAddress">Address, module name and offesets</param>
    /// <param name="newCode">The opcodes to write in the code cave</param>
    /// <param name="instructionOpcodes">The number of bytes of the instruction</param>
    /// <param name="totalAmountOfOpcodes">Because the a x64 jump is 14 bytes large, it will override other instructions, so you have to give this function more to do so.</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Cave address</returns>
    public Task<nuint> CreateOrResumeCodeCaveAsync(MemoryAddress memoryAddress, byte[] newCode, int instructionOpcodes, int totalAmountOfOpcodes, uint size = 0x1000)
    {
        return Task.Run(() => CreateOrResumeCodeCave(memoryAddress, newCode, instructionOpcodes, totalAmountOfOpcodes, size));
    }

    /// <summary>
    /// Creates a code cave to apply custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memoryAddress">Address, module name and offesets</param>
    /// <param name="newCode">The opcodes to write in the code cave</param>
    /// <param name="instructionOpcodes">The number of bytes of the instruction</param>
    /// <param name="totalAmountOfOpcodes">Because the a x64 jump is 14 bytes large, it will override other instructions, so you have to give this function more to do so.</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Cave address</returns>
    public nuint CreateOrResumeCodeCave(MemoryAddress memoryAddress, byte[] newCode, int instructionOpcodes, int totalAmountOfOpcodes, uint size = 0x1000)
    {
        if (instructionOpcodes < 5 || !IsProcessAlive)
        {
            return nuint.Zero;
        }

        if (IsCodeCaveAlreadyCreatedForAddress(memoryAddress, out var caveAddr))
        {
            return caveAddr;
        }

        var targetAddress = GetTargetAddress(memoryAddress);

        CodeCaveFactory.CreateCodeCaveAndInjectCode(targetAddress, _targetProcess.Handle, newCode, instructionOpcodes, totalAmountOfOpcodes,
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