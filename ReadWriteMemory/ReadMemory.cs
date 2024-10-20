using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class RWMemory
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

        if (!GetTargetAddress(memoryAddress, out var targetAddress))
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

    /// <summary>
    /// This method reads the <see cref="string"/> value of <paramref name="length"/> from the specified <paramref name="memoryAddress"/> and stores it in the <paramref name="value"/> parameter.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool ReadString(MemoryAddress memoryAddress, int length, out string value)
    {
        value = string.Empty;

        if (!GetTargetAddress(memoryAddress, out var targetAddress))
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

    /// <summary>
    /// This method will read the <see cref="string"/> from the specified <paramref name="memoryAddress"/> and execute the 
    /// <paramref name="callback"/> function repeatedly with a specified delay <paramref name="refreshTime"/>, using the given 
    /// <paramref name="length"/> as the input parameter.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <param name="ct"></param>
    public void ReadString(MemoryAddress memoryAddress, int length, ReadStringCallback callback, TimeSpan refreshTime, CancellationToken ct)
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            var success = ReadString(memoryAddress, length, out string value);
            callback(success, value);
        }, refreshTime, ct);
    }

    /// <summary>
    /// This method reads the <c>bytes</c> value of <paramref name="length"/> from the specified <paramref name="memoryAddress"/> and stores it in the <paramref name="value"/> parameter.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool ReadBytes(MemoryAddress memoryAddress, int length, out byte[] value)
    {
        value = new byte[length];

        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, value))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This method will read the <c>bytes</c> from the specified <paramref name="memoryAddress"/> and execute the 
    /// <paramref name="callback"/> function repeatedly with a specified delay <paramref name="refreshTime"/>, using the given 
    /// <paramref name="length"/> as the input parameter.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <param name="ct"></param>
    public void ReadBytes(MemoryAddress memoryAddress, int length, ReadBytesCallback callback, TimeSpan refreshTime, CancellationToken ct)
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            var success = ReadBytes(memoryAddress, length, out byte[] value);
            callback(success, value);
        }, refreshTime, ct);
    }
}