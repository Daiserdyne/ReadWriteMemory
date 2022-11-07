using ReadWriteMemory.Models;
using ReadWriteMemory.Services;

namespace ReadWriteMemory;

public sealed partial class Memory
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
        if (!IsProcessAlive())
            return false;

        var targetAddress = CalculateTargetAddress(memoryAddress);

        var buffer = new byte[8];

        if (!ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
        {
            _logger?.Error("Couldn't read value from memory address.");
            return false;
        }

        int tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            _logger?.Info("This value is allready freezed.");
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        switch (refreshRateInMilliseconds)
        {
            case < 5:
                refreshRateInMilliseconds = 5;
                break;

                // Delete this case maybe
            case > int.MaxValue:
                refreshRateInMilliseconds = int.MaxValue;
                break;
        }

        _ = BackgroundService.ExecuteTaskInfiniteAsync(() =>
        {
            if (!WriteProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero))
                freezeToken.Cancel();
        }, TimeSpan.FromMilliseconds(refreshRateInMilliseconds), freezeToken.Token);

        _logger?.Info($"The value of the memory address 0x{(UIntPtr)memoryAddress.Address:x16} has been freezed with " +
            $"a refresh rate of {refreshRateInMilliseconds}ms.");

        return true;
    }

    /// <summary>
    /// Unfreezes a value from the given <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool UnfreezeValue(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive())
            return false;

        int tableIndex = GetAddressIndexByMemoryAddress(memoryAddress);

        if (tableIndex == -1)
        {
            _logger?.Warn("There is no value to unfreeze");
            return false;
        }

        var freezeToken = _addressRegister[tableIndex].FreezeTokenSrc;

        if (freezeToken is null)
        {
            _logger?.Error("There is no value to unfreeze");
            return false;
        }

        freezeToken.Cancel();

        _addressRegister[tableIndex].FreezeTokenSrc = null;

        _logger?.Info($"The value of the memory address 0x{(UIntPtr)memoryAddress.Address:x16} has been unfreezed.");

        return true;
    }
}