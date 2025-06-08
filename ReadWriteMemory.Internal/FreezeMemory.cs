using System.Diagnostics.CodeAnalysis;
using ReadWriteMemory.Internal.Entities;
using ReadWriteMemory.Internal.Services;

namespace ReadWriteMemory.Internal;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public partial class RwMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool FreezeValue<T>(MemoryAddress memoryAddress, T value, TimeSpan freezeRefreshRate) where T : unmanaged
    {
        if (!CheckIfAlreadyFrozen(memoryAddress))
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _memoryRegister[memoryAddress].FreezeTokenSrc = freezeToken;

        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
                WriteValue(memoryAddress, value),
            freezeRefreshRate, freezeToken.Token);

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool FreezeValue<T>(MemoryAddress memoryAddress, TimeSpan freezeRefreshRate) where T : unmanaged
    {
        return ReadValue<T>(memoryAddress, out var value) && 
               FreezeValue(memoryAddress, value, freezeRefreshRate);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public bool FreezeBytes(MemoryAddress memoryAddress, ReadOnlySpan<byte> bytes, TimeSpan freezeRefreshRate)
    {
        if (!CheckIfAlreadyFrozen(memoryAddress))
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _memoryRegister[memoryAddress].FreezeTokenSrc = freezeToken;

        var bytesToFreeze = bytes.ToArray();
        
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
                WriteBytes(memoryAddress, bytesToFreeze),
            freezeRefreshRate, freezeToken.Token);

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <param name="bytesToFreezeLength"></param>
    /// <returns></returns>
    public bool FreezeBytes(MemoryAddress memoryAddress, uint bytesToFreezeLength, TimeSpan freezeRefreshRate)
    {
        return ReadBytes(memoryAddress, bytesToFreezeLength, out var value) && 
               FreezeBytes(memoryAddress, value, freezeRefreshRate);
    }

    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UnfreezeValue(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            return false;
        }

        var freezeToken = table.FreezeTokenSrc;

        if (freezeToken is null)
        {
            return false;
        }

        freezeToken.Cancel();
        freezeToken.Dispose();

        _memoryRegister[memoryAddress].FreezeTokenSrc = null;

        return true;
    }
    
    private bool CheckIfAlreadyFrozen(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (table.FreezeTokenSrc is not null)
        {
            return false;
        }

        return true;
    }
}