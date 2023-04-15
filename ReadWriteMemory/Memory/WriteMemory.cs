using ReadWriteMemory.Models;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Win32 = ReadWriteMemory.NativeImports.Win32;

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
    public bool WriteMemory(MemoryAddress memoryAddress, object value)
    {
        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
        {
            return false;
        }

        if (value is byte[] byteArrayBuffer)
        {
            return WriteProcessMemory(ref targetAddress, ref byteArrayBuffer);
        }

        if (value is string rawStringBuffer)
        {
            var stringBuffer = Encoding.UTF8.GetBytes(rawStringBuffer);
            return WriteProcessMemory(ref targetAddress, ref stringBuffer);
        }

        var length = Marshal.SizeOf(value);

        var buffer = new byte[length];

        var pointer = Marshal.AllocHGlobal(length);

        Marshal.StructureToPtr(value, pointer, true);
        Marshal.Copy(pointer, buffer, 0, length);
        Marshal.FreeHGlobal(pointer);

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
        {
            return false;
        }

        var coordsAddresses = new UIntPtr[3]
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

        for (int i = 0; i < 3; i++)
        {
            var buffer = BitConverter.GetBytes(valuesToWrite[i]);

            if (WriteProcessMemory(ref coordsAddresses[i], ref buffer))
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
        {
            return false;
        }

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

        for (int i = 0; i < 3; i++)
        {
            var buffer = BitConverter.GetBytes(valuesToWrite[i]);

            if (WriteProcessMemory(ref coordsAddresses[i], ref buffer))
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

    private void WriteBytes(UIntPtr address, byte[] buffer)
    {
        if (!IsProcessAlive())
        {
            return;
        }

        Win32.WriteProcessMemory(_targetProcess.Handle, address, buffer, (UIntPtr)buffer.Length, out _);
    }
}