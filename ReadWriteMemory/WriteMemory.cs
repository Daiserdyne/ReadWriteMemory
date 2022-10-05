using ReadWriteMemory.Models;
using System;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    /// <summary>
    /// Writes a byte array to a given address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    private void WriteBytes(UIntPtr address, byte[] write)
    {
        if (_proc is null || _proc.Process.Responding is false)
            return;

        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out _);
    }

    public void WriteMemory(MemoryAddress memAddress, int value)
    {
        var newValue = BitConverter.GetBytes(value);
    }

    public void WriteMemory(MemoryAddress memAddress, long value)
    {
        var newValue = BitConverter.GetBytes(value);
    }

    public void WriteMemory(MemoryAddress memAddress, float value)
    {

    }

    public void WriteMemory(MemoryAddress memAddress, double value)
    {

    }

    public void WriteMemory(MemoryAddress memAddress, string value)
    {

    }
}