using System.Runtime.InteropServices;
using System.Text;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory;

internal abstract class MemoryBase
{
    protected static bool WriteProcessMemory(nint processHandle, nuint targetAddress, object value)
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

        var length = Marshal.SizeOf(value);

        var ptr = Marshal.AllocHGlobal(length);

        Marshal.StructureToPtr(value, ptr, true);

        var valueBuffer = new byte[length];

        Marshal.Copy(ptr, valueBuffer, 0, length);

        Marshal.FreeHGlobal(ptr);

        return WriteProcessMemory(processHandle, targetAddress, valueBuffer);   
    }

    protected static bool WriteProcessMemory(nint processHandle, nuint targetAddress, byte[] buffer)
    {
        return Win32.WriteProcessMemory(processHandle, targetAddress, buffer, buffer.Length, IntPtr.Zero);
    }
}