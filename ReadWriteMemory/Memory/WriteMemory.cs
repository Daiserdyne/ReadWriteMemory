using ReadWriteMemory.Models;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    /// <summary>
    /// Das ist der kleine Buba
    /// </summary>
    /// <param name="memAddress"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool WriteMemory(MemoryAddress memAddress, object value)
    {
        var targetAddress = CalculateTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        if (value is byte[] byteArrayBuffer)
            return WriteProcessMemory(ref targetAddress, ref byteArrayBuffer);

        var length = Marshal.SizeOf(value);

        var buffer = new byte[length];

        var pointer = Marshal.AllocHGlobal(length);

        Marshal.StructureToPtr(value, pointer, true);
        Marshal.Copy(pointer, buffer, 0, length);
        Marshal.FreeHGlobal(pointer);

        _logger?.Info($"Writing value \"{value}\" to target address: 0x{targetAddress:x16} was successfull.");

        return WriteProcessMemory(ref targetAddress, ref buffer);
    }

    /// <summary>
    /// Writes <c>X</c>, <c>Y</c> and <c>Z</c> coordinates to the given memory addresses.
    /// </summary>
    /// <param name="xAddress"></param>
    /// <param name="yAddress"></param>
    /// <param name="zAddress"></param>
    /// <param name="newCoords"></param>
    public bool WriteFloatCoordinates(MemoryAddress xAddress, MemoryAddress yAddress, MemoryAddress zAddress, Vector3 newCoords)
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
        {
            _logger?.Info($"Writing coordinates was successfull.");
            return true;
        }

        _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{Vector3Length} were written.");

        return false;
    }

    /// <summary>
    /// Writes The <c>X</c>, <c>Y</c> and <c>Z</c> coordinates with the given memory-<paramref name="xCoordAddress"/>.
    /// It will take your given <c>X</c> address and add 4 bytes for <c>Y</c> and 8 bytes for <c>Z</c>
    /// to get all three coordinates.
    /// <para><c>Behind the scenes:</c></para>
    /// <example>
    /// <code>var xAddress = <paramref name="xCoordAddress"/></code>
    /// <code>var yAddress = <paramref name="xCoordAddress"/> + 4;</code>
    /// <code>var zAddress = <paramref name="xCoordAddress"/> + 8;</code>
    /// </example>
    /// <para><c>Note: </c>This only works if the coordinate addresses are of type <see cref="float"/> and next to each other in the memory.</para>
    /// </summary>
    /// <param name="xCoordAddress"></param>
    /// <param name="coords"></param>
    public bool WriteFloatCoordinates(MemoryAddress xCoordAddress, Vector3 coords)
    {
        var targetAddress = CalculateTargetAddress(xCoordAddress);

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
        {
            _logger?.Info($"Writing coordinates was successfull.");
            return true;
        }

        _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{Vector3Length} were written.");

        return false;
    }

    /// <summary>
    /// Buggy
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="camRotations"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    //public bool TeleportForward(MemoryAddress memoryAddress, Quaternion camRotations, float distance)
    //{
    //    var targetAddress = CalculateTargetAddress(memoryAddress);

    //    if (targetAddress == UIntPtr.Zero)
    //        return false;

    //    var coordsAddresses = new UIntPtr[3]
    //    {
    //        targetAddress,
    //        targetAddress + 4,
    //        targetAddress + 8
    //    };

    //    var coordValues = new float[3];
    //    int successCounter = 0;

    //    for (int i = 0; i < coordsAddresses.Length; i++)
    //    {
    //        var buffer = new byte[4];

    //        if (ReadProcessMemory(_proc.Handle, coordsAddresses[i], buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
    //            successCounter++;

    //        coordValues[i] = BitConverter.ToSingle(buffer, 0);
    //    }

    //    if (successCounter != coordsAddresses.Length)
    //        return false;

    //    var newPosition = CalculateNewPosition(camRotations, new Vector3(coordValues), distance);

    //    coordValues = new float[Vector3Length]
    //    {
    //        newPosition.X,
    //        newPosition.Y,
    //        newPosition.Z
    //    };

    //    for (int i = 0; i < Vector3Length; i++)
    //    {
    //        var buffer = BitConverter.GetBytes(coordValues[i]);

    //        WriteProcessMemory(ref coordsAddresses[i], ref buffer);
    //    }

    //    //todo: find msg bug
    //    _logger?.Error($"Couldn't write to all coords. Only {successCounter}/{Vector3Length} where written.");

    //    return false;
    //}

    /// <summary>
    /// Writes a byte array to a given address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    private void WriteBytes(UIntPtr address, byte[] write)
    {
        if (!IsProcessAlive())
            return;

        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out _);
    }
}