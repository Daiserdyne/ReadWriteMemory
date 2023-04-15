using ReadWriteMemory.Utilities;
using System.Runtime.InteropServices;
using System.Text;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory;

internal static class MemoryOperation
{
    internal static bool WriteProcessMemoryEx(nint processHandle, nuint targetAddress, object value)
    {
        var valueType = value.GetType();

        if (valueType == typeof(byte[]))
        {
            return WriteProcessMemory(processHandle, targetAddress, (byte[])value);
        }
        else if (valueType == typeof(string))
        {
            var stringAsByteArray = Encoding.UTF8.GetBytes((string)value);
            return WriteProcessMemory(processHandle, targetAddress, stringAsByteArray);
        }

        return WriteValueToProcessMemory(processHandle, targetAddress, value);
    }

    internal static bool WriteProcessMemory(nint processHandle, nuint targetAddress, object value)
    {
        return WriteValueToProcessMemory(processHandle, targetAddress, value);
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

    internal static bool CreateCodeCaveInMemoryAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int replaceCount,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        return CodeCaveFactory.CreateCodeCaveInMemoryAndInjectCode(targetAddress, targetProcessHandle, newCode, replaceCount, out caveAddress, out originalOpcodes, out jmpBytes, size);
    }

    private static bool WriteValueToProcessMemory(nint processHandle, nuint targetAddress, object value)
    {
        var length = Marshal.SizeOf(value);

        var ptr = Marshal.AllocHGlobal(length);

        Marshal.StructureToPtr(value, ptr, true);

        var valueBuffer = new byte[length];

        Marshal.Copy(ptr, valueBuffer, 0, length);

        Marshal.FreeHGlobal(ptr);

        return WriteProcessMemory(processHandle, targetAddress, valueBuffer);
    }
}