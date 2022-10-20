using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using System.Numerics;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    private bool WriteProcessMemory(ref UIntPtr targetAddress, ref byte[] buffer)
    {
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, buffer,
            (UIntPtr)buffer.Length, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
        {
            _logger?.Error(LogMessages.WritingToMemoryFailed);
            return false;
        }

        return true;
    }

    public bool WriteInt16(MemoryAddress memAddress, short value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var buffer = BitConverter.GetBytes(value);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteInt32(MemoryAddress memAddress, int value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var buffer = BitConverter.GetBytes(value);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteInt64(MemoryAddress memAddress, long value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var buffer = BitConverter.GetBytes(value);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteFloat(MemoryAddress memAddress, float value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var buffer = BitConverter.GetBytes(value);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteDouble(MemoryAddress memAddress, double value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var buffer = BitConverter.GetBytes(value);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteString(MemoryAddress memAddress, string value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var buffer = Encoding.UTF8.GetBytes(value);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, float x, float y, float z)
    {
        return WriteCoordinates(memoryAddress, new Vector3(x, y, z));
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, Vector3 coords)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        const int VectorLength = 3;

        var coordsAddresses = new UIntPtr[VectorLength]
        {
            targetAddress,
            targetAddress + 4,
            targetAddress + 8
        };

        var valuesToWrite = new float[VectorLength]
        {
            coords.X,
            coords.Y,
            coords.Z
        };

        int successCounter = 0;

        for (int i = 0; i < VectorLength; i++)
        {
            var buffer = BitConverter.GetBytes(valuesToWrite[i]);

            if (WriteProcessMemory(ref coordsAddresses[i], ref buffer))
                successCounter++;
        }

        if (successCounter == VectorLength)
            return true;

        _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{VectorLength} where written.");

        return false;
    }

    /// <summary>
    /// Writes bytes in to the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress">Target address you want to write to</param>
    /// <param name="bytesToWrite">Byte array to write to</param>
    public bool WriteBytes(MemoryAddress memoryAddress, byte[] bytesToWrite)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        return WriteProcessMemory(ref targetAddress, ref bytesToWrite);
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