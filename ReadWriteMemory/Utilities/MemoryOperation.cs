using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory.Utilities;

internal static class MemoryOperation
{
    internal static bool WriteProcessMemoryEx(nint processHandle, nuint targetAddress, object value)
    {
        switch (value)
        {
            case byte[] buffer:
                return WriteProcessMemory(processHandle, targetAddress, buffer);

            case string str:
                var stringAsByteArray = Encoding.UTF8.GetBytes(str);
                return WriteProcessMemory(processHandle, targetAddress, stringAsByteArray);

            default:
                return WriteProcessMemory(processHandle, targetAddress, value);
        }
    }

    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, object value)
    {
        var length = Marshal.SizeOf(value);

        var ptr = Marshal.AllocHGlobal(length);

        Marshal.StructureToPtr(value, ptr, true);

        var valueBuffer = new byte[length];

        Marshal.Copy(ptr, valueBuffer, 0, length);

        Marshal.FreeHGlobal(ptr);

        return WriteProcessMemory(processHandle, targetAddress, valueBuffer);
    }

    internal static unsafe bool WriteProcessMemory<T>(nint processHandle, nuint targetAddress, T value) where T : unmanaged
    {
        var length = sizeof(T);

        var valueBuffer = length <= 512 ? stackalloc byte[length] : new byte[length];

        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(valueBuffer), value);

        var success = WriteProcessMemory(processHandle, targetAddress, valueBuffer.ToArray());

        if (!success)
        {
            return WriteProcessMemory(processHandle, targetAddress, (object)value);
        }

        return true;
    }

    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer)
    {
        return Win32.WriteProcessMemory(processHandle, targetAddress, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
    }

    internal static bool ReadProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer, UIntPtr size)
    {
        return Win32.ReadProcessMemory(processHandle, targetAddress, buffer, size, IntPtr.Zero);
    }

    internal static bool DeallocateMemory(nint processHandle, nuint address)
    {
        return Win32.VirtualFreeEx(processHandle, address, 0, Win32.MEM_RELEASE);
    }

    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int replaceCount,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        return CodeCaveFactory.CreateCodeCaveAndInjectCode(targetAddress, targetProcessHandle, newCode, replaceCount, out caveAddress, 
            out originalOpcodes, out jmpBytes, size);
    }
}