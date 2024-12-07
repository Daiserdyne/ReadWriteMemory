using ReadWriteMemory.Shared.Entities;
using ReadWriteMemory.Shared.Services;

namespace ReadWriteMemory.Internal;

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
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public unsafe T ReadValue<T>(MemoryAddress memoryAddress) where T : unmanaged
    {
        try
        {
            var targetAddress = GetTargetAddress(memoryAddress);
        
            if (targetAddress == nuint.Zero)
            {
                return default;
            }
        
            return *(T*)(nuint*)targetAddress;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public unsafe byte[] ReadBytes(MemoryAddress memoryAddress, uint length)
    {
        try
        {
            var targetAddress = GetTargetAddress(memoryAddress);

            if (targetAddress == nuint.Zero)
            {
                return [];
            }
        
            var buffer = new byte[length];

            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy((void*)targetAddress, bufferPtr, length, length);
            }

            return buffer;
        }
        catch
        {
            return [];
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool ReadValueConstant<T>(MemoryAddress memoryAddress, ReadValueCallback<T> callback, 
        TimeSpan refreshTime) where T : unmanaged
    {
        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (_memoryRegister[memoryAddress].ReadValueConstantTokenSrc is not null)
        {
            return false;
        }
        
        var readValueConstantTokenSrc = new CancellationTokenSource();

        _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = readValueConstantTokenSrc;
        
        StartReadingValueConstant(memoryAddress, callback, refreshTime, readValueConstantTokenSrc);

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="bytesToRead"></param>
    /// <param name="callback"></param>
    /// <param name="refreshTime"></param>
    /// <returns></returns>
    public bool ReadBytesConstant(MemoryAddress memoryAddress, uint bytesToRead, ReadBytesCallback callback,
        TimeSpan refreshTime)
    {
        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (_memoryRegister[memoryAddress].ReadValueConstantTokenSrc is not null)
        {
            return true;
        }

        var readValueConstantTokenSrc = new CancellationTokenSource();

        _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = readValueConstantTokenSrc;

        StartReadingBytesConstant(memoryAddress, bytesToRead, callback, refreshTime, readValueConstantTokenSrc);

        return true;
    }
    
    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool StopReadingValueConstant(MemoryAddress memoryAddress)
    {
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
    
    private void StartReadingValueConstant<T>(MemoryAddress memoryAddress, ReadValueCallback<T> callback,
        TimeSpan refreshTime, CancellationTokenSource readValueConstantTokenSrc)
        where T : unmanaged
    {
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
        {
            callback(ReadValue<T>(memoryAddress));
        }, refreshTime, readValueConstantTokenSrc.Token);
    }
    
    private void StartReadingBytesConstant(MemoryAddress memoryAddress, uint byteLengthToRead, 
        ReadBytesCallback callback, TimeSpan refreshRate, CancellationTokenSource readValueConstantTokenSrc)
    {
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
        {
            callback(ReadBytes(memoryAddress, byteLengthToRead));
        }, refreshRate, readValueConstantTokenSrc.Token);
    }
}