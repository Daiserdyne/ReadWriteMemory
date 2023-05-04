using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    private const double MaxFreezeRefreshRateInMilliseconds = double.MaxValue;
    private const short MinFreezeRefreshRateInMilliseconds = 5;

    /// <summary>
    /// <para>Freezes the <paramref name="value"/> of an unmanaged data type by the given <paramref name="memoryAddress"/> with a 
    /// given <paramref name="freezeRefreshRate"></paramref>.</para>
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <returns></returns>
    public bool FreezeValue<T>(MemoryAddress memoryAddress, T value, TimeSpan freezeRefreshRate) where T : unmanaged
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        freezeRefreshRate = GetFreezeRefreshRate(freezeRefreshRate);

        var freezeValue = MemoryOperation.ConvertToByteArrayUnsafe(value);

        StartFreezingValue(memoryAddress, freezeRefreshRate, targetAddress, freezeValue, freezeToken, tableIndex);

        return true;
    }

    /// <summary>
    /// Freezes the value of an unmanaged data type by the given <paramref name="memoryAddress"/> with a
    /// given <paramref name="freezeRefreshRate"></paramref>. The value will be read out once and then applied to to 
    /// <paramref name="memoryAddress"/>. You have the specify the data type <typeparamref name="T"/> to get the size of the buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <returns></returns>
    public unsafe bool FreezeValue<T>(MemoryAddress memoryAddress, TimeSpan freezeRefreshRate) where T : unmanaged
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        var freezeValue = new byte[sizeof(T)];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, freezeValue))
        {
            return false;
        }

        StartFreezingValue(memoryAddress, freezeRefreshRate, targetAddress, freezeValue, freezeToken, tableIndex);

        return true;
    }

    /// <summary>
    /// Freezes the value by the given <paramref name="memoryAddress"/> with a
    /// given <paramref name="freezeRefreshRate"></paramref>. The value will be read out once and then applied to to 
    /// <paramref name="memoryAddress"/>. This overload allows you the set the <paramref name="bufferSize"/> by yourself.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="bufferSize"></param>
    /// <param name="freezeRefreshRate"></param>
    /// <returns></returns>
    public bool FreezeValue(MemoryAddress memoryAddress, uint bufferSize, TimeSpan freezeRefreshRate)
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[bufferSize];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        var tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        freezeRefreshRate = GetFreezeRefreshRate(freezeRefreshRate);

        StartFreezingValue(memoryAddress, freezeRefreshRate, targetAddress, buffer, freezeToken, tableIndex);

        return true;
    }

    private void StartFreezingValue(MemoryAddress memoryAddress, TimeSpan freezeRefreshRate, 
        nuint targetAddress, byte[] buffer, CancellationTokenSource freezeToken, int tableIndex)
    {
        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            if (!GetTargetAddress(memoryAddress, out targetAddress)
            || !MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                freezeToken.Cancel();

                _addressRegister[tableIndex].FreezeTokenSrc = null;

                return;
            }
        }, freezeRefreshRate, freezeToken.Token);
    }

    private static TimeSpan GetFreezeRefreshRate(TimeSpan freezeRefreshRate)
    {
        switch (Math.Round(freezeRefreshRate.TotalMilliseconds, 0))
        {
            case < MinFreezeRefreshRateInMilliseconds:
                freezeRefreshRate = TimeSpan.FromMilliseconds(MinFreezeRefreshRateInMilliseconds);
                break;

            case > double.MaxValue:
                freezeRefreshRate = TimeSpan.FromMilliseconds(MaxFreezeRefreshRateInMilliseconds);
                break;
        }

        return freezeRefreshRate;
    }

    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UnfreezeValue(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive)
        {
            return false;
        }

        var tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        if (tableIndex == -1)
        {
            return false;
        }

        var freezeToken = _addressRegister[tableIndex].FreezeTokenSrc;

        if (freezeToken is null)
        {
            return false;
        }

        freezeToken.Cancel();

        _addressRegister[tableIndex].FreezeTokenSrc = null;

        return true;
    }
}