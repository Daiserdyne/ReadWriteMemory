using ReadWriteMemory.External.Utilities;
using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.External;

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
        if (!BytesAlreadyReplaced(memoryAddress))
        {
            return false;
        }

        var buffer = new byte[replacement.Length];

        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, replacement))
        {
            _memoryRegister[memoryAddress].ReplacedBytes = new()
            {
                OriginalOpcodes = buffer
            };

            return true;
        }

        return false;
    }

    private bool BytesAlreadyReplaced(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (table.ReplacedBytes is not null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Writes the original opcodes to the address you replaced with <see cref="ReplaceBytes"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UndoReplaceBytes(MemoryAddress memoryAddress)
    {
        if (BytesAlreadyReplaced(memoryAddress))
        {
            return false;
        }

        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }
        
        if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress,
                _memoryRegister[memoryAddress].ReplacedBytes!.Value.OriginalOpcodes))
        {
            _memoryRegister[memoryAddress].ReplacedBytes = null;

            return true;
        }

        return false;
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