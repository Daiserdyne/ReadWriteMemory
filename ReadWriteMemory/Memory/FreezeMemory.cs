using ReadWriteMemory.Models;
using ReadWriteMemory.Services;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public bool FreezeInt16(MemoryAddress memAddress, uint refreshRateInMilliseconds = 100)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToFreeze = ReadProcessMemory(targetAddress, 2);

        if (valueToFreeze is null)
            return false;

        int tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            _logger?.Error("", "This value is allread freezed");
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        refreshRateInMilliseconds = refreshRateInMilliseconds 
            < 10 ? 10 : refreshRateInMilliseconds;

        BackgroundService.ExecuteTaskAsync(() =>
        {
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
            if (!WriteProcessMemory(_proc.Handle, targetAddress, valueToFreeze, (UIntPtr)2, IntPtr.Zero))
                freezeToken.Cancel();
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
        }, TimeSpan.FromMilliseconds(refreshRateInMilliseconds), freezeToken.Token);

        return true;
    }

    private byte[]? ReadProcessMemory(UIntPtr targetAddress, int size)
    {
        if (!IsProcessAliveAndResponding())
            return null;

        var valueToFreeze = new byte[size];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (!ReadProcessMemory(_proc.Handle, targetAddress, valueToFreeze, (UIntPtr)4, IntPtr.Zero))
        {
            _logger?.Error("Error", "Couldn't read value from memory address.");
            return null;
        }
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        return valueToFreeze;
    }

    public bool FreezeFloat(MemoryAddress memAddress, uint refreshRateInMilliseconds = 100)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = new byte[4];

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        if (!ReadProcessMemory(_proc.Handle, targetAddress, valueToWrite, (UIntPtr)4, IntPtr.Zero))
        {
            _logger?.Error("", "Couldn't read value from memory address.");
            return false;
        }
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        int tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            _logger?.Info("", "This value is allready freezed.");
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        refreshRateInMilliseconds = refreshRateInMilliseconds 
            < 10 ? 10 : refreshRateInMilliseconds;

        BackgroundService.ExecuteTaskAsync(() =>
        {
            if (!WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite, (UIntPtr)4, IntPtr.Zero))
                freezeToken.Cancel();
        }, TimeSpan.FromMilliseconds(refreshRateInMilliseconds), freezeToken.Token);

        return true;
    }

    public bool UnfreezeValue(MemoryAddress memAddress)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        int tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            _logger?.Warn("WARN", "There is no value to unfreeze");
            return false;
        }

        var freezeToken = _addressRegister[tableIndex].FreezeTokenSrc;

        if (freezeToken is null)
        {
            _logger?.Error("", "There is no value to unfreeze");
            return false;
        }

        freezeToken.Cancel();

        _addressRegister[tableIndex].FreezeTokenSrc = null;

        return true;
    }
}