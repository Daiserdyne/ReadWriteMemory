using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshRateInMilliseconds"/>
    /// to a specific value you want.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="refreshRateInMilliseconds"></param>
    /// <returns></returns>
    public bool FreezeValue(MemoryAddress memoryAddress, uint refreshRateInMilliseconds = 100)
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

        switch (refreshRateInMilliseconds)
        {
            case < 5:
                refreshRateInMilliseconds = 5;
                break;

            case > int.MaxValue:
                refreshRateInMilliseconds = int.MaxValue;
                break;
        }

        _ = BackgroundService.ExecuteTaskInfinite(() =>
        {
            if (!MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                freezeToken.Cancel();
            }
        }, TimeSpan.FromMilliseconds(refreshRateInMilliseconds), freezeToken.Token);

        return true;
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshRateInMilliseconds"/>
    /// to a specific value you want.
    /// Don't forget to specify the <paramref name="freezeValue"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeValue"></param>
    /// <param name="refreshRateInMilliseconds"></param>
    /// <returns></returns>
    public bool ChangeAndFreezeValue(MemoryAddress memoryAddress, string freezeValue, uint refreshRateInMilliseconds = 100)
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, freezeValue);

        return FreezeValue(memoryAddress, refreshRateInMilliseconds);
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshRateInMilliseconds"/>
    /// to a specific value you want.
    /// Don't forget to specify the <paramref name="freezeValue"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeValue"></param>
    /// <param name="refreshRateInMilliseconds"></param>
    /// <returns></returns>
    public bool ChangeAndFreezeValue(MemoryAddress memoryAddress, byte[] freezeValue, uint refreshRateInMilliseconds = 100)
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, freezeValue);

        return FreezeValue(memoryAddress, refreshRateInMilliseconds);
    }

    /// <summary>
    /// <para>Freezes the value from the given <paramref name="memoryAddress"/>.</para>
    /// You optionally can set the <paramref name="refreshRateInMilliseconds"/>
    /// to a specific value you want.
    /// This version of ChangeAndFreezeValue will only accepts unmanaged data types.
    /// Don't forget to specify the <paramref name="freezeValue"/> type. For example if you want to write a float, add the 'f' behind the number, for
    /// double add a 'd' so that the memory knows what type you want to write.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="freezeValue"></param>
    /// <param name="refreshRateInMilliseconds"></param>
    /// <returns></returns>
    public bool ChangeAndFreezeValue<T>(MemoryAddress memoryAddress, T freezeValue, uint refreshRateInMilliseconds = 100) where T : unmanaged
    {
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            return false;
        }

        MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, freezeValue);

        return FreezeValue(memoryAddress, refreshRateInMilliseconds);
    }

    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UnfreezeValue(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive())
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