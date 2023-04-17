using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;
using System.Numerics;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    #region Delegates

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="wasReadingSuccessfull"></param>
    public delegate void ReadValueCallback(bool wasReadingSuccessfull, object? value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coords"></param>
    /// <param name="wasReadingSuccessfull"></param>
    public delegate void ReadCoordinates(bool wasReadingSuccessfull, Vector3 coords);

    #endregion

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
    /// <returns>The value of the address, parsed to the given <see cref="MemoryDataTypes"/>. If the function fails, it will return <c>null</c>.</returns>
    public bool ReadProcessMemory(MemoryAddress memoryAddress, MemoryDataTypes type, out object? value, int readBufferSize = 8)
    {
        value = null!;

        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[readBufferSize];

        if (MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            ConvertTargetValue(type, buffer, ref value);

            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="type"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <param name="ct"></param>
    /// <param name="readBufferSize"></param>
    public void ReadProcessMemory(MemoryAddress memoryAddress, MemoryDataTypes type, ReadValueCallback callback, TimeSpan refreshTime, CancellationToken ct, int readBufferSize = 8)
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            var success = ReadProcessMemory(memoryAddress, type, out var value, readBufferSize);
            callback(success, value);
        }, refreshTime, ct);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="xPosition"></param>
    /// <param name="yPosition"></param>
    /// <param name="zPosition"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <param name="ct"></param>
    public void ReadFloatCoordinates(MemoryAddress xPosition, MemoryAddress yPosition, MemoryAddress zPosition, ReadCoordinates callback,
        TimeSpan refreshTime, CancellationToken ct)
    {
        bool success;

        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            success = ReadFloatCoordinates(xPosition, yPosition, zPosition, out var coordinates);
            callback(success, coordinates);
        }, refreshTime, ct);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="xPosition"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <param name="ct"></param>
    public void ReadFloatCoordinates(MemoryAddress xPosition, ReadCoordinates callback, TimeSpan refreshTime, CancellationToken ct)
    {
        bool success;

        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            success = ReadFloatCoordinates(xPosition, out var coordinates);
            callback(success, coordinates);
        }, refreshTime, ct);
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
    public unsafe bool ReadFloatCoordinates(MemoryAddress xPosition, MemoryAddress yPosition, MemoryAddress zPosition, out Vector3 coordinates)
    {
        coordinates = Vector3.Zero;

        if (!IsProcessAlive())
        {
            return false;
        }

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

        short successCounter = 0;

        var buffer = new byte[4];

        for (short i = 0; i < 3; i++)
        {
            if (MemoryOperation.ReadProcessMemory(_targetProcess.Handle, coordsAddresses[i], buffer))
            {
                if (MemoryOperation.GetValueUnsafe<float>(buffer, out var value))
                {
                    successCounter++;
                    coordValues[i] = value;
                }

                successCounter++;
            }
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
    /// <code>var xAddress = <paramref name="xCoordinateMemoryAddress"/></code>
    /// <code>var yAddress = <paramref name="xCoordinateMemoryAddress"/> + 4;</code>
    /// <code>var zAddress = <paramref name="xCoordinateMemoryAddress"/> + 8;</code>
    /// </example>
    /// <para><c>Note: </c>This only works if the coordinate addresses are of type <see cref="float"/> and next to each other in the memory.</para>
    /// </summary>
    /// <param name="xCoordinateMemoryAddress"></param>
    /// <param name="coordinates"></param>
    /// <returns>A <see cref="Vector3"/> struct where the read coords are stored. If the function fails, 
    /// it returns an empty <see cref="Vector3"/>.</returns>
    public unsafe bool ReadFloatCoordinates(MemoryAddress xCoordinateMemoryAddress, out Vector3 coordinates)
    {
        coordinates = Vector3.Zero;

        if (!CheckProcStateAndGetTargetAddress(xCoordinateMemoryAddress, out var targetAddress))
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

        short successCounter = 0;

        var buffer = new byte[4];

        for (short i = 0; i < 3; i++)
        {
            if (MemoryOperation.ReadProcessMemory(_targetProcess.Handle, coordsAddresses[i], buffer))
            {
                if (MemoryOperation.GetValueUnsafe<float>(buffer, out var value))
                {
                    successCounter++;
                    coordValues[i] = value;
                }
            }
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