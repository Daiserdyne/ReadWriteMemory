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
    /// A function pointer that will be called after reading the string value.
    /// </summary>
    /// <param name="wasReadingSuccessfull"></param>
    /// <param name="stringValue"></param>
    public delegate void ReadStringCallback(bool wasReadingSuccessfull, string stringValue);

    /// <summary>
    /// A function pointer that will be called after reading the byte array value.
    /// </summary>
    /// <param name="wasReadingSuccessfull"></param>
    /// <param name="byteArrayValue"></param>
    public delegate void ReadBytesCallback(bool wasReadingSuccessfull, byte[] byteArrayValue);

    /// <summary>
    /// A function pointer that will be called after reading the unmanaged value.
    /// </summary>
    /// <param name="wasReadingSuccessfull"></param>
    /// <param name="unmanagedValue"></param>
    public delegate void ReadValueCallback<T>(bool wasReadingSuccessfull, T unmanagedValue) where T : unmanaged;

    /// <summary>
    /// A function pointer that will be called after reading the coordinates.
    /// </summary>
    /// <param name="wasReadingSuccessfull"></param>
    /// <param name="coords"></param>
    public delegate void ReadCoordinatesCallback(bool wasReadingSuccessfull, Vector3 coords);

    #endregion

    /// <summary>
    /// This will read the <paramref name="value"/> out of the given <paramref name="memoryAddress"/>.
    /// Don't forget to specify the <typeparamref name="T"/> type correctly to prevent errors or unintended outcomes.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <returns>A <seealso cref="bool"/> indicating whether the operation was successful.</returns>
    public unsafe bool ReadValue<T>(MemoryAddress memoryAddress, out T value) where T : unmanaged
    {
        value = default;

        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[sizeof(T)];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        if (!MemoryOperation.ConvertBufferUnsafe(buffer, out value))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This will read the <typeparamref name="T"/>-value and executes the <paramref name="callback"/>
    /// function in a loop with the specified delay <paramref name="refreshTime"/>.
    /// Don't forget to specify the <typeparamref name="T"/> type correctly to prevent errors or unintended outcomes.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <param name="ct"></param>
    public void ReadValue<T>(MemoryAddress memoryAddress, ReadValueCallback<T> callback, TimeSpan refreshTime, CancellationToken ct) where T : unmanaged
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            var success = ReadValue<T>(memoryAddress, out var value);
            callback(success, value);
        }, refreshTime, ct);
    }

    public bool ReadString(MemoryAddress memoryAddress, int length, out string value)
    {
        value = string.Empty;

        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[length];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        try
        {
            value = Encoding.UTF8.GetString(buffer, 0, length);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public void ReadString(MemoryAddress memoryAddress, int length, ReadStringCallback callback, TimeSpan refreshTime, CancellationToken ct)
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            var success = ReadString(memoryAddress, length, out string value);
            callback(success, value);
        }, refreshTime, ct);
    }

    public bool ReadBytes(MemoryAddress memoryAddress, int length, out byte[] value)
    {
        value = new byte[length];

        if (!CheckProcStateAndGetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, value))
        {
            return false;
        }

        return true;
    }

    public void ReadBytes(MemoryAddress memoryAddress, int length, ReadBytesCallback callback, TimeSpan refreshTime, CancellationToken ct)
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            var success = ReadBytes(memoryAddress, length, out byte[] value);
            callback(success, value);
        }, refreshTime, ct);
    }

    ///// <summary>
    ///// Reads out the <c>X</c>, <c>Y</c> and <c>Z</c> coordinates with the given memory addresses.
    ///// </summary>
    ///// <param name="xPosition"></param>
    ///// <param name="yPosition"></param>
    ///// <param name="zPosition"></param>
    ///// <param name="coordinates"></param>
    ///// <returns>A <see cref="Vector3"/> struct where the read coords are stored. If the function fails, 
    ///// it returns an empty <see cref="Vector3"/>.</returns>
    //public unsafe bool ReadFloatCoordinates(MemoryAddress xPosition, MemoryAddress yPosition, MemoryAddress zPosition, out Vector3 coordinates)
    //{
    //    coordinates = Vector3.Zero;

    //    if (!IsProcessAlive())
    //    {
    //        return false;
    //    }

    //    var xAddress = CalculateTargetAddress(xPosition);
    //    var yAddress = CalculateTargetAddress(yPosition);
    //    var zAddress = CalculateTargetAddress(zPosition);

    //    if (xAddress == UIntPtr.Zero || yAddress == UIntPtr.Zero || zAddress == UIntPtr.Zero)
    //    {
    //        return false;
    //    }

    //    var coordsAddresses = new UIntPtr[3]
    //    {
    //        xAddress,
    //        yAddress,
    //        zAddress
    //    };

    //    var coordValues = new float[3];

    //    short successCounter = 0;

    //    var buffer = new byte[4];

    //    for (short i = 0; i < 3; i++)
    //    {
    //        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, coordsAddresses[i], buffer))
    //        {
    //            break;
    //        }

    //        if (MemoryOperation.ConvertBufferUnsafe<float>(buffer, out var value))
    //        {
    //            successCounter++;
    //            coordValues[i] = value;
    //        }
    //    }

    //    if (successCounter != 3)
    //    {
    //        return false;
    //    }

    //    coordinates.X = coordValues[0];
    //    coordinates.Y = coordValues[1];
    //    coordinates.Z = coordValues[2];

    //    return true;
    //}

    //public void ReadFloatCoordinates(MemoryAddress xPosition, MemoryAddress yPosition, MemoryAddress zPosition, ReadCoordinatesCallback callback, 
    //    TimeSpan refreshTime, CancellationToken ct)
    //{
    //    bool success;

    //    _ = BackgroundService.ExecuteTaskInfinite(() =>
    //    {
    //        success = ReadFloatCoordinates(xPosition, yPosition, zPosition, out var coordinates);
    //        callback(success, coordinates);
    //    }, refreshTime, ct);
    //}

    ///// <summary>
    ///// Reads out the <c>X</c>, <c>Y</c> and <c>Z</c> coordinates with the given memory-<paramref name="xCoordAddress"/>.
    ///// It will take your given <c>X</c> address and add 4 bytes for <c>Y</c> and 8 bytes for <c>Z</c>
    ///// to get all three coordinates.
    ///// <para><c>Behind the scenes:</c></para>
    ///// <example>
    ///// <code>var xAddress = <paramref name="xCoordinateMemoryAddress"/></code>
    ///// <code>var yAddress = <paramref name="xCoordinateMemoryAddress"/> + 4;</code>
    ///// <code>var zAddress = <paramref name="xCoordinateMemoryAddress"/> + 8;</code>
    ///// </example>
    ///// <para><c>Note: </c>This only works if the coordinate addresses are of type <see cref="float"/> and next to each other in the memory.</para>
    ///// </summary>
    ///// <param name="xCoordinateMemoryAddress"></param>
    ///// <param name="coordinates"></param>
    ///// <returns>A <see cref="Vector3"/> struct where the read coords are stored. If the function fails, 
    ///// it returns an empty <see cref="Vector3"/>.</returns>
    //public unsafe bool ReadFloatCoordinates(MemoryAddress xCoordinateMemoryAddress, out Vector3 coordinates)
    //{
    //    coordinates = Vector3.Zero;

    //    if (!CheckProcStateAndGetTargetAddress(xCoordinateMemoryAddress, out var targetAddress))
    //    {
    //        return false;
    //    }

    //    var coordsAddresses = new UIntPtr[3]
    //    {
    //        targetAddress,
    //        targetAddress + 4,
    //        targetAddress + 8
    //    };

    //    var coordValues = new float[3];

    //    short successCounter = 0;

    //    var buffer = new byte[4];

    //    for (short i = 0; i < 3; i++)
    //    {
    //        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, coordsAddresses[i], buffer))
    //        {
    //            break;
    //        }

    //        if (MemoryOperation.ConvertBufferUnsafe<float>(buffer, out var value))
    //        {
    //            successCounter++;
    //            coordValues[i] = value;
    //        }
    //    }

    //    if (successCounter != 3)
    //    {
    //        return false;
    //    }

    //    coordinates.X = coordValues[0];
    //    coordinates.Y = coordValues[1];
    //    coordinates.Z = coordValues[2];

    //    return true;
    //}

    //public void ReadFloatCoordinates(MemoryAddress xCoordinateMemoryAddress, ReadCoordinatesCallback callback, TimeSpan refreshTime, CancellationToken ct)
    //{
    //    bool success;

    //    _ = BackgroundService.ExecuteTaskInfinite(() =>
    //    {
    //        success = ReadFloatCoordinates(xCoordinateMemoryAddress, out var coordinates);
    //        callback(success, coordinates);
    //    }, refreshTime, ct);
    //}
}