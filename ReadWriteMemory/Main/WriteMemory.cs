using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    /// <summary>
    /// This will write the given <seealso cref="string"/>-<paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns>A <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public bool WriteString(MemoryAddress memoryAddress, string value)
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        return MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
    }

    /// <summary>
    /// This will write the given <seealso cref="string"/>-<paramref name="value"/> with the given <paramref name="length"/> 
    /// to the target <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <param name="length"></param>
    /// <returns>A <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public bool WriteString(MemoryAddress memoryAddress, string value, int length)
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        return MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value, length);
    }

    /// <summary>
    /// This will write the given <c>ByteArray</c>-<paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns>A <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public bool WriteBytes(MemoryAddress memoryAddress, byte[] value)
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        return MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
    }

    /// <summary>
    /// This will write the given <paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// Don't forget to specify the <paramref name="value"/> type to prevent errors or unintended outcomes.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns>A <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public bool WriteValue<T>(MemoryAddress memoryAddress, T value) where T : unmanaged
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        return MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
    }
}