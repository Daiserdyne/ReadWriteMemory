using ReadWriteMemory.Models;
using System;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    /// <summary>
    /// Writes a byte array to a given address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    private void WriteBytes(UIntPtr address, byte[] write)
    {
        if (_proc is null || _proc.Process.Responding is false)
            return;

        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out _);
    }

    public bool WriteMemory(MemoryAddress memAddress, DataType type, object value)
    {
        if (_proc is null)
            return false;

        byte[]? newValue = null;
        int size = 4;

        switch (type)
        {
            case DataType.Short:

                if (value is short)
                {
                    newValue = BitConverter.GetBytes((short)value);
                    size = 2;
                }

                break;

            case DataType.Int:

                if (value is int)
                {
                    newValue = BitConverter.GetBytes((int)value);
                    size = 4;
                }

                break;

            case DataType.Long:

                if (value is long)
                {
                    newValue = BitConverter.GetBytes((long)value);
                    size = 8;
                }

                break;

            case DataType.Float:

                if (value is float)
                {
                    newValue = BitConverter.GetBytes((float)value);
                    size = 4;
                }

                break;

            case DataType.Double:

                if (value is double)
                {
                    newValue = BitConverter.GetBytes((double)value);
                    size = 8;
                }

                break;

            case DataType.String:

                if (value is string)
                {
                    newValue = Encoding.UTF8.GetBytes((string)value);
                    size = newValue.Length;
                }

                break;

            default:
                return false;
        }

        if (newValue == null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        return WriteProcessMemory(_proc.Handle, targetAddress, newValue, (UIntPtr)size, IntPtr.Zero);
    }
}