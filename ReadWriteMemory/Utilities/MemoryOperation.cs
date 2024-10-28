using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Kernel32 = ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class MemoryOperation
{
    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, string value)
    {
        var stringAsByteArray = Encoding.UTF8.GetBytes(value);
        return WriteProcessMemory(processHandle, targetAddress, stringAsByteArray);
    }

    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, string value, int length)
    {
        var stringAsByteArray = Encoding.UTF8.GetBytes(value);
        return WriteProcessMemory(processHandle, targetAddress, stringAsByteArray, length);
    }

    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer)
    {
        return Kernel32.WriteProcessMemory(processHandle, targetAddress, buffer, (nuint)buffer.Length, out _);
    }

    private static bool WriteProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer, int length)
    {
        return Kernel32.WriteProcessMemory(processHandle, targetAddress, buffer, (nuint)length, out _);
    }

    internal static bool WriteProcessMemory<T>(nint processHandle, nuint targetAddress, T value) where T : unmanaged
    {
        var valueBuffer = ConvertToByteArrayUnsafe(value);

        return WriteProcessMemory(processHandle, targetAddress, valueBuffer);
    }

    internal static bool ReadProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer)
    {
        return Kernel32.ReadProcessMemory(processHandle, targetAddress, buffer, buffer.Length, nint.Zero);
    }

    internal static unsafe byte[] ConvertToByteArrayUnsafe<T>(T value) where T : unmanaged
    {
        var buffer = new byte[sizeof(T)];

        fixed (byte* pByte = buffer)
        {
            *(T*)pByte = value;
        }

        return buffer;
    }

    internal static unsafe bool ConvertBufferUnsafe<T>(byte[] buffer, out T value) where T : unmanaged
    {
        if (sizeof(T) != buffer.Length)
        {
            value = default;

            return false;
        }

        fixed (byte* pByte = buffer)
        {
            value = *(T*)pByte;
        }

        return true;
    }
    
    internal static unsafe bool ConvertBufferUnsafeRef<T>(byte[] buffer, ref T value) where T : unmanaged
    {
        if (sizeof(T) != buffer.Length)
        {
            value = default;

            return false;
        }

        fixed (byte* pByte = buffer)
        {
            value = *(T*)pByte;
        }

        return true;
    }

    internal static bool DeallocateMemory(nint processHandle, nuint address)
    {
        return Kernel32.VirtualFreeEx(processHandle, address, 0, Kernel32.MEM_RELEASE);
    }
}