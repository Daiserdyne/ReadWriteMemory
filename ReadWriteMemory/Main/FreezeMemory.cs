using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;
using System.Text;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set a <paramref name="refreshTime"/>
    /// to a specific value you want.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="valueToFreeze"></param>
    /// <param name="refreshTime"></param>
    /// <returns></returns>
    public bool FreezeValue<T>(MemoryAddress memoryAddress, T valueToFreeze, TimeSpan refreshTime) where T : unmanaged
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

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        switch (Math.Round(refreshTime.TotalMilliseconds, 0))
        {
            case < 1:
                refreshTime = TimeSpan.FromMilliseconds(1);
                break;

            case > double.MaxValue:
                refreshTime = TimeSpan.FromMilliseconds(double.MaxValue);
                break;
        }

        var value = MemoryOperation.ConvertToByteArrayUnsafe(valueToFreeze);

        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            if (!GetTargetAddress(memoryAddress, out targetAddress)
            || !MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, value))
            {
                tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);
                _addressRegister[tableIndex].FreezeTokenSrc = null;

                freezeToken.Cancel();

                return;
            }
        }, refreshTime, freezeToken.Token);

        return true;
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set a <paramref name="refreshTime"/>
    /// to a specific value you want.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="refreshTime"></param>
    /// <returns></returns>
    public bool FreezeValue(MemoryAddress memoryAddress, TimeSpan refreshTime)
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[8];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        var tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        switch (refreshTime.TotalMilliseconds)
        {
            case < 5:
                refreshTime = TimeSpan.FromMilliseconds(5);
                break;

            case > double.MaxValue:
                refreshTime = TimeSpan.FromMilliseconds(100);
                break;
        }

        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            if (!MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                freezeToken.Cancel();
            }
        }, refreshTime, freezeToken.Token);

        return true;
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set a <paramref name="refreshTime"/>
    /// to a specific value you want.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="refreshTime"></param>
    /// <param name="valueAsBytes"></param>
    /// <returns></returns>
    private bool FreezeValue(MemoryAddress memoryAddress, TimeSpan refreshTime, byte[] valueAsBytes)
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

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        switch (refreshTime.TotalMilliseconds)
        {
            case < 5:
                refreshTime = TimeSpan.FromMilliseconds(5);
                break;

            case > double.MaxValue:
                refreshTime = TimeSpan.FromMilliseconds(100);
                break;
        }

        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            if (!MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, valueAsBytes))
            {
                freezeToken.Cancel();
            }
        }, refreshTime, freezeToken.Token);

        return true;
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshTime"/>
    /// to a specific value you want.
    /// Don't forget to specify the <paramref name="freezeValue"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeValue"></param>
    /// <param name="refreshTime"></param>
    /// <returns></returns>
    public bool ChangeAndFreezeValue(MemoryAddress memoryAddress, string freezeValue, TimeSpan refreshTime)
    {
        if (string.IsNullOrEmpty(freezeValue))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(freezeValue);

        return FreezeValue(memoryAddress, refreshTime, bytes);
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshTime"/>
    /// to a specific value you want.
    /// Don't forget to specify the <paramref name="freezeValue"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeValue"></param>
    /// <param name="refreshTime"></param>
    /// <returns></returns>
    public bool ChangeAndFreezeValue(MemoryAddress memoryAddress, byte[] freezeValue, TimeSpan refreshTime)
    {
        return FreezeValue(memoryAddress, refreshTime, freezeValue);
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshTime"/>
    /// to a specific value you want.
    /// This version of ChangeAndFreezeValue will only accepts unmanaged data types.
    /// Don't forget to specify the <paramref name="freezeValue"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeValue"></param>
    /// <param name="refreshTime"></param>
    /// <returns></returns>
    public bool ChangeAndFreezeValue<T>(MemoryAddress memoryAddress, T freezeValue, TimeSpan refreshTime) where T : unmanaged
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, freezeValue);

        return FreezeValue(memoryAddress, refreshTime);
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