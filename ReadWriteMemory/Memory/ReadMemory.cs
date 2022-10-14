using ReadWriteMemory.Models;
using System.Numerics;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public short? ReadInt16(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[2];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)2, IntPtr.Zero))
            return BitConverter.ToInt16(valueToRead, 0);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        return null;
    }

    public int? ReadInt32(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[4];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)4, IntPtr.Zero))
            return BitConverter.ToInt32(valueToRead, 0);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        return null;
    }

    public long? ReadInt64(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[8];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)8, IntPtr.Zero))
            return BitConverter.ToInt64(valueToRead, 0);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        return null;
    }

    public float? ReadFloat(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[4];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)4, IntPtr.Zero))
            return BitConverter.ToSingle(valueToRead, 0);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        return null;
    }

    public double? ReadDouble(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
            return null;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var valueToRead = new byte[8];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (ReadProcessMemory(_proc.Handle, targetAddress, valueToRead, (UIntPtr)8, IntPtr.Zero))
            return BitConverter.ToDouble(valueToRead, 0);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        return null;
    }

    public Vector3? ReadCoordinates(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive())
            return null;

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var coordsAddresses = new UIntPtr[3]
        {
            targetAddress,
            targetAddress + 4,
            targetAddress + 8
        };

        var coordValues = new float[3];
        int successCounter = 0;

        for (int i = 0; i < coordsAddresses.Length; i++)
        {
            var valueToRead = new byte[8];
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
            if (ReadProcessMemory(_proc.Handle, coordsAddresses[i], valueToRead, (UIntPtr)8, IntPtr.Zero))
                successCounter++;
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

            coordValues[i] = BitConverter.ToSingle(valueToRead, 0);
        }

        if (successCounter != coordsAddresses.Length)
            return null;

        return new Vector3(coordValues);
    }
}