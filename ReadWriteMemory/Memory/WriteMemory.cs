using ReadWriteMemory.Models;
using System.Numerics;
using System.Text;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    public bool WriteInt16(MemoryAddress memAddress, short value)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)2, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
            return false;

        return true;
    }

    public bool WriteInt32(MemoryAddress memAddress, int value)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)4, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
            return false;

        return true;
    }

    public bool WriteInt64(MemoryAddress memAddress, long value)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)8, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
            return false;

        return true;
    }

    public bool WriteFloat(MemoryAddress memAddress, float value)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)4, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
            return false;

        return true;
    }

    public bool WriteDouble(MemoryAddress memAddress, double value)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = BitConverter.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)8, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
            return false;

        return true;
    }

    public bool WriteString(MemoryAddress memAddress, string value)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var valueToWrite = Encoding.UTF8.GetBytes(value);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
        var success = WriteProcessMemory(_proc.Handle, targetAddress, valueToWrite,
            (UIntPtr)valueToWrite.Length, IntPtr.Zero);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

        if (!success)
            return false;

        return true;
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, float x, float y, float z)
    {
        return WriteCoordinates(memoryAddress, new Vector3(x, y, z));
    }

    public bool WriteCoordinates(MemoryAddress memoryAddress, Vector3 coords)
    {
        if (!IsProcessAliveAndResponding())
            return false;

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
            return false;

        var coordsAddresses = new UIntPtr[3]
        {
            targetAddress,
            targetAddress + 4,
            targetAddress + 8
        };

        var valuesToWrite = new float[3]
        {
            coords.X,
            coords.Y,
            coords.Z
        };

        int successCounter = 0;

        for (int i = 0; i < coordsAddresses.Length; i++)
        {
            var valueToWrite = BitConverter.GetBytes(valuesToWrite[i]);

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
            if (WriteProcessMemory(_proc.Handle, coordsAddresses[i], valueToWrite,
                (UIntPtr)valueToWrite.Length, IntPtr.Zero))
            {
                successCounter++;
            }
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
        }

        if (successCounter == coordsAddresses.Length)
            return true;

        return false;
    }

    //public bool TeleportForward(Quaternion rotation, Vector3 position, float distance)
    //{
    //    if (!IsProcessAliveAndResponding())
    //        return false;



    //    return false;
    //}
}