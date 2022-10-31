using ReadWriteMemory.Models;
using System.Numerics;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public short? ReadInt16(MemoryAddress memoryAddress)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var buffer = new byte[2];

#pragma warning disable CS8602
        if (ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            return BitConverter.ToInt16(buffer, 0);
#pragma warning restore CS8602

        return null;
    }

    public int? ReadInt32(MemoryAddress memoryAddress)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var buffer = new byte[4];

#pragma warning disable CS8602
        if (ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            return BitConverter.ToInt32(buffer, 0);
#pragma warning restore CS8602

        return null;
    }

    public long? ReadInt64(MemoryAddress memoryAddress)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var buffer = new byte[8];

#pragma warning disable CS8602
        if (ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            return BitConverter.ToInt64(buffer, 0);
#pragma warning restore CS8602

        return null;
    }

    public float? ReadFloat(MemoryAddress memoryAddress)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var buffer = new byte[4];

#pragma warning disable CS8602
        if (ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            return BitConverter.ToSingle(buffer, 0);
#pragma warning restore CS8602

        return null;
    }

    public double? ReadDouble(MemoryAddress memoryAddress)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return null;

        var buffer = new byte[8];

#pragma warning disable CS8602
        if (ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            return BitConverter.ToDouble(buffer, 0);
#pragma warning restore CS8602

        return null;
    }

    public Vector3? ReadCoordinates(MemoryAddress xPosition, MemoryAddress yPosition, MemoryAddress zPosition)
    {
        var xAddress = CalculateTargetAddress(xPosition);
        var yAddress = CalculateTargetAddress(yPosition);
        var zAddress = CalculateTargetAddress(zPosition);

        if (xAddress == UIntPtr.Zero || yAddress == UIntPtr.Zero || zAddress == UIntPtr.Zero)
            return null;

        var coordsAddresses = new UIntPtr[3]
        {
            xAddress,
            yAddress,
            zAddress
        };

        var coordValues = new float[3];
        int successCounter = 0;

        for (int i = 0; i < coordsAddresses.Length; i++)
        {
            var buffer = new byte[4];
#pragma warning disable CS8602
            if (ReadProcessMemory(_proc.Handle, coordsAddresses[i], buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
                successCounter++;
#pragma warning restore CS8602

            coordValues[i] = BitConverter.ToSingle(buffer, 0);
        }

        if (successCounter != coordsAddresses.Length)
            return null;

        return new Vector3(coordValues);
    }

    public Vector3? ReadCoordinates(MemoryAddress memoryAddress)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

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
            var buffer = new byte[4];
#pragma warning disable CS8602
            if (ReadProcessMemory(_proc.Handle, coordsAddresses[i], buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
                successCounter++;
#pragma warning restore CS8602

            coordValues[i] = BitConverter.ToSingle(buffer, 0);
        }

        if (successCounter != coordsAddresses.Length)
            return null;

        return new Vector3(coordValues);
    }
}