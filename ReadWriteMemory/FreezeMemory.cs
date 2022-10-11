using ReadWriteMemory.Models;
using ReadWriteMemory.Services;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public bool FreezeInt16(MemoryAddress memAddress, short value, uint refreshRateInMilliseconds = 100)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

        int tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            _logger?.Error("", "This value is allread freezed");
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        refreshRateInMilliseconds = refreshRateInMilliseconds < 10 ? 10 : refreshRateInMilliseconds;

        BackgroundService.ExecuteTaskAsync(() =>
        {
            WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite, (UIntPtr)2, IntPtr.Zero);
        }, TimeSpan.FromMilliseconds(refreshRateInMilliseconds), freezeToken.Token);

        return true;
    }

    public bool FreezeFloat(MemoryAddress memAddress, uint refreshRateInMilliseconds = 100)
    {
        if (_proc is null)
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = new byte[4];

        if (!ReadProcessMemory(_proc.Handle, targetAddress, valueToWrite, (UIntPtr)4, IntPtr.Zero))
        {
            _logger?.Error("", "Couldn't read value from memory address.");
            return false;
        }

        int tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (_addressRegister[tableIndex].FreezeTokenSrc is not null)
        {
            _logger?.Error("", "This value is allread freezed");
            return false;
        }

        var freezeToken = new CancellationTokenSource();

        _addressRegister[tableIndex].FreezeTokenSrc = freezeToken;

        refreshRateInMilliseconds = refreshRateInMilliseconds < 10 ? 10 : refreshRateInMilliseconds;

        BackgroundService.ExecuteTaskAsync(() =>
        {
            WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite, (UIntPtr)4, IntPtr.Zero);
        }, TimeSpan.FromMilliseconds(refreshRateInMilliseconds), freezeToken.Token);

        return true;
    }

    public bool UnfreezeValue(MemoryAddress memAddress)
    {
        int tableIndex = GetAddressIndexByMemoryAddress(memAddress);

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