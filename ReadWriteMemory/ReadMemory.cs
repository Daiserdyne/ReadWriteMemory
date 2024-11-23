using ReadWriteMemory.Entities;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;

// ReSharper disable MemberCanBePrivate.Global

namespace ReadWriteMemory;

public partial class RwMemory
{
    #region Delegates

    /// <summary>
    /// A delegate that will be called after reading the unmanaged value.
    /// </summary>
    /// <param name="unmanagedValue"></param>
    public delegate void ReadValueCallback<in T>(T unmanagedValue) where T : unmanaged;

    /// <summary>
    /// A delegate that will be called after reading the byte array value.
    /// </summary>
    /// <param name="byteArrayValue"></param>
    public delegate void ReadBytesCallback(byte[] byteArrayValue);

    #endregion

    private unsafe void InitAndStartConstantValueReading<T>(MemoryAddress memoryAddress, ReadValueCallback<T> callback,
        TimeSpan readValueRate) where T : unmanaged
    {
        var readValueConstantTokenSrc = new CancellationTokenSource();

        _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = readValueConstantTokenSrc;

        var buffer = new byte[sizeof(T)];

        StartReadingValueConstant(memoryAddress, callback, readValueRate, buffer, readValueConstantTokenSrc);
    }

    private void InitAndStartConstantByteReading(MemoryAddress memoryAddress, byte[] buffer, ReadBytesCallback callback,
        TimeSpan readValueRate)
    {
        var readValueConstantTokenSrc = new CancellationTokenSource();

        _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = readValueConstantTokenSrc;

        StartReadingBytesConstant(memoryAddress, callback, readValueRate, buffer, readValueConstantTokenSrc);
    }

    private void StartReadingBytesConstant(MemoryAddress memoryAddress, ReadBytesCallback callback,
        TimeSpan readValueRate, byte[] buffer, CancellationTokenSource readValueConstantTokenSrc)
    {
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
        {
            if (!GetTargetAddress(memoryAddress, out var targetAddress)
                || !MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                readValueConstantTokenSrc.Cancel();
                readValueConstantTokenSrc.Dispose();

                _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = null;
            }
            else
            {
                callback.Invoke(buffer);
            }
        }, readValueRate, readValueConstantTokenSrc.Token);
    }

    private void StartReadingValueConstant<T>(MemoryAddress memoryAddress, ReadValueCallback<T> callback,
        TimeSpan readValueRate, byte[] buffer, CancellationTokenSource readValueConstantTokenSrc)
        where T : unmanaged
    {
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
        {
            if (!GetTargetAddress(memoryAddress, out var targetAddress)
                || !MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                readValueConstantTokenSrc.Cancel();
                readValueConstantTokenSrc.Dispose();

                _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = null;
            }
            else
            {
                MemoryOperation.ConvertBufferUnsafe(buffer, out T value);
                callback.Invoke(value);
            }
        }, readValueRate, readValueConstantTokenSrc.Token);
    }

    private bool CheckIfAlreadyReadingConstant(MemoryAddress memoryAddress)
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress) ||
            _memoryRegister[memoryAddress].ReadValueConstantTokenSrc is not null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool StopReadingValueConstant(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive)
        {
            return false;
        }

        if (!_memoryRegister.TryGetValue(memoryAddress, out var value))
        {
            return false;
        }

        var readValueConstantToken = value.ReadValueConstantTokenSrc;

        if (readValueConstantToken is null)
        {
            return false;
        }

        readValueConstantToken.Cancel();
        readValueConstantToken.Dispose();

        _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = null;

        return true;
    }

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
    /// This method reads the <c>bytes</c> value of <paramref name="length"/> from the specified <paramref name="memoryAddress"/>
    /// and stores it in the <paramref name="value"/> parameter.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool ReadBytes(MemoryAddress memoryAddress, uint length, out byte[] value)
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
    /// <paramref name="bytesToRead"/> as the input parameter.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="bytesToRead"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    public bool ReadBytesConstant(MemoryAddress memoryAddress, uint bytesToRead, ReadBytesCallback callback,
        TimeSpan refreshTime)
    {
        if (!CheckIfAlreadyReadingConstant(memoryAddress))
        {
            return false;
        }

        var buffer = new byte[bytesToRead];

        InitAndStartConstantByteReading(memoryAddress, buffer, callback, refreshTime);

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
    public bool ReadValueConstant<T>(MemoryAddress memoryAddress, ReadValueCallback<T> callback, TimeSpan refreshTime)
        where T : unmanaged
    {
        if (!CheckIfAlreadyReadingConstant(memoryAddress))
        {
            return false;
        }

        InitAndStartConstantValueReading(memoryAddress, callback, refreshTime);

        return true;
    }
}