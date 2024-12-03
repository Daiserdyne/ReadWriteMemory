﻿using ReadWriteMemory.Shared.Entities;
using ReadWriteMemory.Shared.Services;

namespace ReadWriteMemory.Internal;

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
        if (_memoryRegister[memoryAddress].FreezeTokenSrc is not null)
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
        return FreezeValue(memoryAddress, ReadValue<T>(memoryAddress), freezeRefreshRate);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public bool FreezeBytes(MemoryAddress memoryAddress, byte[] bytes, TimeSpan freezeRefreshRate)
    {
        if (_memoryRegister[memoryAddress].FreezeTokenSrc is not null)
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _memoryRegister[memoryAddress].FreezeTokenSrc = freezeToken;

        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
                WriteBytes(memoryAddress, bytes),
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
        return FreezeBytes(memoryAddress, ReadBytes(memoryAddress, bytesToFreezeLength), freezeRefreshRate);
    }

    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UnfreezeValue(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var value))
        {
            return false;
        }

        var freezeToken = value.FreezeTokenSrc;

        if (freezeToken is null)
        {
            return false;
        }

        freezeToken.Cancel();
        freezeToken.Dispose();

        _memoryRegister[memoryAddress].FreezeTokenSrc = null;

        return true;
    }
}