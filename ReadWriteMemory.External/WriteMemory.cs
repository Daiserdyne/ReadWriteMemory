using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Utilities;

namespace ReadWriteMemory.External;

public partial class RwMemory
{
    /// <summary>
    /// This will write the given <c>ByteArray</c>-<paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns>An <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public bool WriteBytes(MemoryAddress memoryAddress, ReadOnlySpan<byte> value)
    {
        return GetTargetAddress(memoryAddress, out var targetAddress) &&
               MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
    }

    /// <summary>
    /// This will write the given <paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// Don't forget to specify the <paramref name="value"/> type to prevent errors or unintended outcomes.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns>An <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public bool WriteValue<T>(MemoryAddress memoryAddress, T value) where T : unmanaged
    {
        return GetTargetAddress(memoryAddress, out var targetAddress) &&
               MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
    }
}