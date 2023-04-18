using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using System.Numerics;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    /// <summary>
    /// This will write the given <paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// Don't forget to specify the type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool WriteString(MemoryAddress memoryAddress, string value)
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        return MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
    }

    /// <summary>
    /// This will write the given <paramref name="value"/> to the target <paramref name="memoryAddress"/>.
    /// Don't forget to specify the type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns></returns>
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
    /// This version of WriteProcessMemory will only accepts unmanaged data types.
    /// Don't forget to specify the <paramref name="value"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool WriteValue<T>(MemoryAddress memoryAddress, T value) where T : unmanaged
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        return MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value);
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
        if (!IsProcessAlive())
        {
            return false;
        }

        var targetXAddress = CalculateTargetAddress(xAddress);
        var targetYAddress = CalculateTargetAddress(yAddress);
        var targetZAddress = CalculateTargetAddress(zAddress);

        if (targetXAddress == nuint.Zero || targetYAddress == nuint.Zero || targetZAddress == nuint.Zero)
        {
            return false;
        }

        var coordsAddresses = new nuint[3]
        {
            targetXAddress,
            targetYAddress,
            targetZAddress
        };

        var valuesToWrite = new float[3]
        {
            newCoords.X,
            newCoords.Y,
            newCoords.Z
        };

        int successCounter = 0;

        for (short i = 0; i < 3; i++)
        {
            if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, coordsAddresses[i], valuesToWrite[i]))
            {
                successCounter++;
            }
        }

        if (successCounter == 3)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes The <c>X</c>, <c>Y</c> and <c>Z</c> coordinates with the given memory-<paramref name="memoryAddress"/>.
    /// It will take your given <c>X</c> address and add 4 bytes for <c>Y</c> and 8 bytes for <c>Z</c>
    /// to get all three coordinates.
    /// <para><c>Behind the scenes:</c></para>
    /// <example>
    /// <code>var xAddress = <paramref name="memoryAddress"/></code>
    /// <code>var yAddress = <paramref name="memoryAddress"/> + 4;</code>
    /// <code>var zAddress = <paramref name="memoryAddress"/> + 8;</code>
    /// </example>
    /// <para><c>Note: </c>This only works if the coordinate addresses are of type <see cref="float"/> and next to each other in the memory.</para>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="coords"></param>
    public bool WriteFloatCoordinates(MemoryAddress memoryAddress, Vector3 coords)
    {
        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        if (targetAddress == nuint.Zero)
        {
            return false;
        }

        var coordsAddresses = new nuint[3]
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

        for (short i = 0; i < 3; i++)
        {
            if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, coordsAddresses[i], valuesToWrite[i]))
            {
                successCounter++;
            }
        }

        if (successCounter == 3)
        {
            return true;
        }

        return false;
    }
}