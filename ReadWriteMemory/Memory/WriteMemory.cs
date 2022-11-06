using ReadWriteMemory.Models;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    const int Vector3Length = 3;

    public bool WriteMemory(MemoryAddress memAddress, object value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var length = Marshal.SizeOf(value);

        var buffer = new byte[length];

        var pointer = Marshal.AllocHGlobal(length);

        Marshal.StructureToPtr(value, pointer, true);
        Marshal.Copy(pointer, buffer, 0, length);
        Marshal.FreeHGlobal(pointer);

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, float newXCoord, float newYCoord, float newZCoord)
    {
        return WriteCoordinates(memoryAddress, new Vector3(newXCoord, newYCoord, newZCoord));
    }

    public bool WriteCoordinates(MemoryAddress xAddress, MemoryAddress yAddress, MemoryAddress zAddress, 
        float newXCoord, float newYCoord, float newZCoord)
    {
        return WriteCoordinates(xAddress, yAddress, zAddress, new Vector3(newXCoord, newYCoord, newZCoord));
    }

    public bool WriteCoordinates(MemoryAddress xAddress, MemoryAddress yAddress, MemoryAddress zAddress, Vector3 newCoords)
    {
        var targetXAddress = CalculateTargetAddress(xAddress);
        var targetYAddress = CalculateTargetAddress(yAddress);
        var targetZAddress = CalculateTargetAddress(zAddress);

        if (targetXAddress == UIntPtr.Zero || targetYAddress == UIntPtr.Zero || targetZAddress == UIntPtr.Zero)
            return false;

        var coordsAddresses = new UIntPtr[Vector3Length]
        {
            targetXAddress,
            targetYAddress,
            targetZAddress
        };

        var valuesToWrite = new float[Vector3Length]
        {
            newCoords.X,
            newCoords.Y,
            newCoords.Z
        };

        int successCounter = 0;

        for (int i = 0; i < Vector3Length; i++)
        {
            var buffer = BitConverter.GetBytes(valuesToWrite[i]);

            if (WriteProcessMemory(ref coordsAddresses[i], ref buffer))
                successCounter++;
        }

        if (successCounter == Vector3Length)
            return true;

        _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{Vector3Length} where written.");

        return false;
    }

    public bool WriteCoordinates(MemoryAddress xAddress, Vector3 coords)
    {
        var targetAddress = CalculateTargetAddress(xAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var coordsAddresses = new UIntPtr[Vector3Length]
        {
            targetAddress,
            targetAddress + 4,
            targetAddress + 8
        };

        var valuesToWrite = new float[Vector3Length]
        {
            coords.X,
            coords.Y,
            coords.Z
        };

        int successCounter = 0;

        for (int i = 0; i < Vector3Length; i++)
        {
            var buffer = BitConverter.GetBytes(valuesToWrite[i]);

            if (WriteProcessMemory(ref coordsAddresses[i], ref buffer))
                successCounter++;
        }

        if (successCounter == Vector3Length)
            return true;

        _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{Vector3Length} where written.");

        return false;
    }

    /// <summary>
    /// Buggy
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="camRotations"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool TeleportForward(MemoryAddress memoryAddress, Quaternion camRotations, float distance)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

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
            return false;

        var newPosition = CalculateNewPosition(camRotations, new Vector3(coordValues), distance);

        coordValues = new float[Vector3Length]
        {
            newPosition.X,
            newPosition.Y,
            newPosition.Z
        };

        for (int i = 0; i < Vector3Length; i++)
        {
            var buffer = BitConverter.GetBytes(coordValues[i]);

            WriteProcessMemory(ref coordsAddresses[i], ref buffer);
        }

        //todo: find msg bug
        _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{Vector3Length} where written.");

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