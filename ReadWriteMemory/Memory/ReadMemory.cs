using ReadWriteMemory.Models;
using System.Numerics;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    #region Enums

    /// <summary>
    /// A enum of all supported <see cref="Memory"/> data types.
    /// </summary>
    public enum MemoryDataTypes : short
    {
        /// <summary>
        /// Represents an <see cref="System.Int16"/>. (2 bytes large)
        /// </summary>
        Int16,
        /// <summary>
        /// Represents an <see cref="System.Int32"/>. (4 bytes large)
        /// </summary>
        Int32,
        /// <summary>
        /// Represents an <see cref="System.Int64"/>. (8 bytes large)
        /// </summary>
        Int64,
        /// <summary>
        /// Represents an <see cref="System.Single"/>. (4 bytes large)
        /// </summary>
        Float,
        /// <summary>
        /// Represents an <see cref="System.Double"/>. (8 bytes large)
        /// </summary>
        Double,
        /// <summary>
        /// Represents an <see cref="System.String"/>.
        /// </summary>
        String,
        /// <summary>
        /// Represents an array of <see cref="System.Byte"/>.
        /// </summary>
        ByteArray
    }

    #endregion

    /// <summary>
    /// This will read out the <paramref name="value"/> of the given <see cref="MemoryAddress"/> and returns the read value in the given type.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <param name="readBufferSize"></param>
    /// <returns>The value of the address, parsed to the given <see cref="MemoryDataTypes"/>. If the function fails, it will return <c>0</c>.</returns>
    public bool ReadMemory(MemoryAddress memoryAddress, MemoryDataTypes type, out object value, int readBufferSize = 8)
    {
        value = 0;

        var targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
        {
            return false;
        }

        var buffer = new byte[readBufferSize];

        if (ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
        {
            ConvertTargetValue(type, buffer, ref value);

            _logger?.Info($"Reading value \"{value}\" from target address: 0x{targetAddress:x16} was successfull.");

            return true;
        }

        return false;
    }

    private static void ConvertTargetValue(MemoryDataTypes type, byte[] buffer, ref object value) 
    {
        value = type switch
        {
            MemoryDataTypes.Int16 => BitConverter.ToInt16(buffer, 0),
            MemoryDataTypes.Int32 => BitConverter.ToInt32(buffer, 0),
            MemoryDataTypes.Int64 => BitConverter.ToInt64(buffer, 0),
            MemoryDataTypes.Float => BitConverter.ToSingle(buffer, 0),
            MemoryDataTypes.Double => BitConverter.ToDouble(buffer, 0),
            MemoryDataTypes.String => Encoding.UTF8.GetString(buffer),
            MemoryDataTypes.ByteArray => buffer,
            _ => throw new ArgumentException("Invalid type", nameof(type))
        };
    }

    /// <summary>
    /// Reads out the <c>X</c>, <c>Y</c> and <c>Z</c> coordinates with the given memory addresses.
    /// </summary>
    /// <param name="xPosition"></param>
    /// <param name="yPosition"></param>
    /// <param name="zPosition"></param>
    /// <param name="coordinates"></param>
    /// <returns>A <see cref="Vector3"/> struct where the read coords are stored. If the function fails, 
    /// it returns an empty <see cref="Vector3"/>.</returns>
    public bool ReadFloatCoordinates(MemoryAddress xPosition, MemoryAddress yPosition, MemoryAddress zPosition, out Vector3 coordinates)
    {
        coordinates = new();

        var xAddress = CalculateTargetAddress(xPosition);
        var yAddress = CalculateTargetAddress(yPosition);
        var zAddress = CalculateTargetAddress(zPosition);

        if (xAddress == UIntPtr.Zero || yAddress == UIntPtr.Zero || zAddress == UIntPtr.Zero)
        {
            return false;
        }

        var coordsAddresses = new UIntPtr[3]
        {
            xAddress,
            yAddress,
            zAddress
        };

        var coordValues = new float[3];

        int successCounter = 0;

        for (int i = 0; i < 3; i++)
        {
            var buffer = new byte[4];

            if (ReadProcessMemory(_targetProcess.Handle, coordsAddresses[i], buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            {
                successCounter++;
            }

            coordValues[i] = BitConverter.ToSingle(buffer, 0);
        }

        if (successCounter != 3)
        {
            return false;
        }

        coordinates.X = coordValues[0];
        coordinates.Y = coordValues[1];
        coordinates.Z = coordValues[2];

        return true;
    }

    /// <summary>
    /// Reads out the <c>X</c>, <c>Y</c> and <c>Z</c> coordinates with the given memory-<paramref name="xCoordAddress"/>.
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
    /// <param name="coordinates"></param>
    /// <returns>A <see cref="Vector3"/> struct where the read coords are stored. If the function fails, 
    /// it returns an empty <see cref="Vector3"/>.</returns>
    public bool ReadFloatCoordinates(MemoryAddress xCoordAddress, out Vector3 coordinates)
    {
        coordinates = new();

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

        var coordValues = new float[3];

        int successCounter = 0;

        for (int i = 0; i < 3; i++)
        {
            var buffer = new byte[4];

            if (ReadProcessMemory(_targetProcess.Handle, coordsAddresses[i], buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
            {
                successCounter++;
            }

            coordValues[i] = BitConverter.ToSingle(buffer, 0);
        }

        if (successCounter != 3)
        {
            return false;
        }

        coordinates.X = coordValues[0];
        coordinates.Y = coordValues[1];
        coordinates.Z = coordValues[2];

        return true;
    }
}