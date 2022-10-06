using ReadWriteMemory.Models;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public bool WriteInt16(MemoryAddress memAddress, short value)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)2, IntPtr.Zero);

        if (!success)
            return false;

        return true;
    }

    public bool WriteInt32(MemoryAddress memAddress, int value)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)4, IntPtr.Zero);

        if (!success)
            return false;

        return true;
    }

    public bool WriteInt64(MemoryAddress memAddress, long value)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)8, IntPtr.Zero);

        if (!success)
            return false;

        return true;
    }

    public bool WriteFloat(MemoryAddress memAddress, float value)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)4, IntPtr.Zero);

        if (!success)
            return false;

        return true;
    }

    public bool WriteDouble(MemoryAddress memAddress, double value)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)8, IntPtr.Zero);

        if (!success)
            return false;

        return true;
    }

    public bool WriteString(MemoryAddress memAddress, string value)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = Encoding.UTF8.GetBytes(value);

        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)valueToWrite.Length, IntPtr.Zero);

        if (!success)
            return false;

        return true;
    }
}