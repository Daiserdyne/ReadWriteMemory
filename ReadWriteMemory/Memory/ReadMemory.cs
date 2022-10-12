using ReadWriteMemory.Models;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public short? ReadInt16(MemoryAddress memAddress)
    {
        if (_proc is null)
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[2];

        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)2, IntPtr.Zero))
            return BitConverter.ToInt16(valueToRead, 0);

        return null;
    }

    public int? ReadInt32(MemoryAddress memAddress)
    {
        if (_proc is null)
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[4];

        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)4, IntPtr.Zero))
            return BitConverter.ToInt32(valueToRead, 0);

        return null;
    }

    public long? ReadInt64(MemoryAddress memAddress)
    {
        if (_proc is null)
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[8];

        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)8, IntPtr.Zero))
            return BitConverter.ToInt64(valueToRead, 0);

        return null;
    }

    public float? ReadFloat(MemoryAddress memAddress)
    {
        if (_proc is null)
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[4];

        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)4, IntPtr.Zero))
            return BitConverter.ToSingle(valueToRead, 0);

        return null;
    }

    public double? ReadDouble(MemoryAddress memAddress)
    {
        if (_proc is null)
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[8];

        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)8, IntPtr.Zero))
            return BitConverter.ToDouble(valueToRead, 0);

        return null;
    }
}