using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using System.Numerics;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public bool WriteInt16(MemoryAddress memAddress, short value)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)2, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteInt32(MemoryAddress memAddress, int value)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)4, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteInt64(MemoryAddress memAddress, long value)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)8, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteFloat(MemoryAddress memAddress, float value)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)4, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteDouble(MemoryAddress memAddress, double value)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)8, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteString(MemoryAddress memAddress, string value)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = Encoding.UTF8.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)valueToWrite.Length, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, float x, float y, float z)
    {
        return WriteCoordinates(memoryAddress, new Vector3(x, y, z));
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, Vector3 coords)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var coordsAddresses = new UIntPtr[3]
        {
            targetAddress,
            targetAddress + 4,
            targetAddress + 8
        };

        var valuesToWrite = new float[3]
        {
            coords.X,
            coords.Y,
            coords.Z
        };

        int successCounter = 0;

        for (int i = 0; i < coordsAddresses.Length; i++)
        {
            var valueToWrite = BitConverter.GetBytes(valuesToWrite[i]);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
            if (WriteProcessMemory(_proc.Handle, coordsAddresses[i], valueToWrite,
                (UIntPtr)valueToWrite.Length, IntPtr.Zero))
            {
                successCounter++;
            }
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
        }

        if (successCounter == coordsAddresses.Length)
            return true;

        _logger?.Error(LogMessages.WritingToMemoryFailed);

        return false;
    }

    /// <summary>
    /// Writes a byte array to a given address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    public bool WriteBytes(MemoryAddress memoryAddress, byte[] write)
    {
        if (!IsProcessAlive())
            return false;

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success =  WriteProcessMemory(_proc.Handle, targetAddress, write, (UIntPtr)write.Length, out _);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Writes a byte array to a given address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    private void WriteBytes(UIntPtr address, byte[] write)
    {
        if (!IsProcessAlive())
            return;

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out _);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
    }
}