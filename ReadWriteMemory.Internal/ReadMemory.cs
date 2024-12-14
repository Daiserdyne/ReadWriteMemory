using System.Diagnostics.CodeAnalysis;
using ReadWriteMemory.Internal.Entities;
using ReadWriteMemory.Internal.Services;

namespace ReadWriteMemory.Internal;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
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
    public delegate void ReadBytesCallback(ReadOnlySpan<byte> byteArrayValue);

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public unsafe bool ReadValue<T>(MemoryAddress memoryAddress, out T value) where T : unmanaged
    {
        try
        {
            var targetAddress = GetTargetAddress(memoryAddress);
        
            value = *(T*)(nuint*)targetAddress;

            return true;
        }
        catch
        {
            value = default;
            
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public unsafe bool ReadBytes(MemoryAddress memoryAddress, uint length, out ReadOnlySpan<byte> value)
    {
        try
        {
            var targetAddress = GetTargetAddress(memoryAddress);
            
            var buffer = new byte[length];

            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy((void*)targetAddress, bufferPtr, 
                    length, length);
            }

            value = buffer;
            
            return true;
        }
        catch
        {
            value = [];
            
            return false;
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
        if (!IsAlreadyReadingConstant(memoryAddress))
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
        if (!IsAlreadyReadingConstant(memoryAddress))
        {
            return false;
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
            if (!ReadValue<T>(memoryAddress, out var value))
            {
                readValueConstantTokenSrc.Cancel();
                readValueConstantTokenSrc.Dispose();
                
                _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = null;

                return;
            }
            
            callback(value);
        }, refreshTime, readValueConstantTokenSrc.Token);
    }
    
    private void StartReadingBytesConstant(MemoryAddress memoryAddress, uint byteLengthToRead, 
        ReadBytesCallback callback, TimeSpan refreshRate, CancellationTokenSource readValueConstantTokenSrc)
    {
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
        {
            if (!ReadBytes(memoryAddress, byteLengthToRead, out var value))
            {
                readValueConstantTokenSrc.Cancel();
                readValueConstantTokenSrc.Dispose();
                
                _memoryRegister[memoryAddress].ReadValueConstantTokenSrc = null;

                return;
            }
            
            callback(value);
        }, refreshRate, readValueConstantTokenSrc.Token);
    }
    
    private bool IsAlreadyReadingConstant(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var value))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (value.ReadValueConstantTokenSrc is not null)
        {
            return false;
        }

        return true;
    }
}