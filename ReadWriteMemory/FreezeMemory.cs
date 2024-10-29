using ReadWriteMemory.Entities;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory;

public partial class RwMemory
{
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
        if (!IsFreezingPossible(memoryAddress, out _))
        {
            return false;
        }

        var buffer = MemoryOperation.ConvertToByteArrayUnsafe(value);

        InitAndStartFreezeProcedure(memoryAddress, freezeRefreshRate, buffer);

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
        if (!IsFreezingPossible(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[sizeof(T)];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        InitAndStartFreezeProcedure(memoryAddress, freezeRefreshRate, buffer);

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
    public bool FreezeValue(MemoryAddress memoryAddress, TimeSpan freezeRefreshRate, uint bufferSize)
    {
        if (!IsFreezingPossible(memoryAddress, out var targetAddress))
        {
            return false;
        }

        var buffer = new byte[bufferSize];

        if (!MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer))
        {
            return false;
        }

        InitAndStartFreezeProcedure(memoryAddress, freezeRefreshRate, buffer);

        return true;
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

    private void StartFreezingValue(MemoryAddress memoryAddress, TimeSpan freezeRefreshRate,
        byte[] buffer, CancellationTokenSource freezeToken)
    {
        _ = BackgroundService.ExecuteTaskRepeatedly(() =>
        {
            if (!GetTargetAddress(memoryAddress, out var targetAddress)
                || !MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, buffer))
            {
                freezeToken.Cancel();
                freezeToken.Dispose();
                
                _memoryRegister[memoryAddress].FreezeTokenSrc = null;
            }
        }, freezeRefreshRate, freezeToken.Token);
    }

    private bool IsFreezingPossible(MemoryAddress memoryAddress, out nuint targetAddress)
    {
        if (!GetTargetAddress(memoryAddress, out targetAddress) &&
            _memoryRegister[memoryAddress].FreezeTokenSrc is not null)
        {
            return false;
        }

        return true;
    }

    private void InitAndStartFreezeProcedure(MemoryAddress memoryAddress, TimeSpan freezeRefreshRate, byte[] buffer)
    {
        var freezeToken = new CancellationTokenSource();

        _memoryRegister[memoryAddress].FreezeTokenSrc = freezeToken;

        StartFreezingValue(memoryAddress, freezeRefreshRate, buffer, freezeToken);
    }
}