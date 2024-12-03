using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    /// <summary>
    /// Basically the same as <see cref="WriteBytes"/> with the difference, that the original
    /// opcodes will be saved internally. So you don't have to save the original opcodes somewhere.
    /// Just call <see cref="UndoReplaceBytes"/> to restore the original opcodes.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="replacement"></param>
    /// <returns></returns>
    public bool ReplaceBytes(MemoryAddress memoryAddress, byte[] replacement)
    {
        if (_memoryRegister[memoryAddress].ReplacedBytes is not null)
        {
            return false;
        }

        _memoryRegister[memoryAddress].ReplacedBytes = new()
        {
            OriginalOpcodes = ReadBytes(memoryAddress, (uint)replacement.Length)
        };
        
        WriteBytes(memoryAddress, replacement);

        return true;
    }
    
    /// <summary>
    /// Writes the original opcodes to the address you replaced with <see cref="ReplaceBytes"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UndoReplaceBytes(MemoryAddress memoryAddress)
    {
        if (_memoryRegister[memoryAddress].ReplacedBytes is null)
        {
            return false;
        }

        var originalBytes = _memoryRegister[memoryAddress]
            .ReplacedBytes!
            .Value
            .OriginalOpcodes;
        
        WriteBytes(memoryAddress, originalBytes);
        
        _memoryRegister[memoryAddress].ReplacedBytes = null;

        return true;
    }
    
    private void RestoreAllReplacedBytes()
    {
        foreach (var (memoryAddress, table) in _memoryRegister)
        {
            if (table.ReplacedBytes is not null)
            {
                UndoReplaceBytes(memoryAddress);
            }
        }
    }
}