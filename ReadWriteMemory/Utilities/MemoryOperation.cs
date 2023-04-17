using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory.Utilities;

internal static class MemoryOperation
{
    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, string value)
    {
        var stringAsByteArray = Encoding.UTF8.GetBytes(value);
        return WriteProcessMemory(processHandle, targetAddress, stringAsByteArray);
    }

    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer)
    {
        return Win32.WriteProcessMemory(processHandle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
    }

    internal static unsafe bool WriteProcessMemory<T>(nint processHandle, nuint targetAddress, T value) where T : unmanaged
    {
        var length = sizeof(T);

        Span<byte> valueBuffer = stackalloc byte[length];

        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(valueBuffer), value);

        return WriteProcessMemory(processHandle, targetAddress, valueBuffer.ToArray());
    }

    internal static bool ReadProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer)
    {
        return Win32.ReadProcessMemory(processHandle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
    }

    internal static unsafe bool ConvertBufferUnsafe<T>(byte[] buffer, out T value) where T : unmanaged
    {
        var size = sizeof(T);

        if (size != buffer.Length)
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
        return Win32.VirtualFreeEx(processHandle, address, 0, Win32.MEM_RELEASE);
    }
}