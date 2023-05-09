using System.Runtime.InteropServices;
using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory2
{
    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int replaceCount,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        caveAddress = nuint.Zero;

        var memInfo = new MEMORY_BASIC_INFORMATION64();

        var freeRegion = targetAddress;

        while (VirtualQueryEx(targetProcessHandle, freeRegion, out memInfo, (uint)Marshal.SizeOf(memInfo)) != 0)
        {
            if (memInfo.State == MEM_FREE && memInfo.RegionSize >= size) // Replace with your desired memory block size
            {
                break;
            }

            freeRegion = memInfo.BaseAddress - (nuint)memInfo.RegionSize;
        }

        caveAddress = VirtualAllocEx(targetProcessHandle, memInfo.BaseAddress, 0x1000, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        var nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        var offset = (int)((long)caveAddress - (long)targetAddress - 5);

        jmpBytes = new byte[5 + nopsNeeded];

        jmpBytes[0] = 0xE9;

        Buffer.BlockCopy(MemoryOperation.ConvertToByteArrayUnsafe(offset), 0, jmpBytes, 1, sizeof(int));

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        var caveBytes = new byte[5 + newCode.Length];

        offset = (int)((long)caveAddress + jmpBytes.Length - ((long)caveAddress + newCode.Length) - 5);

        Buffer.BlockCopy(newCode, 0, caveBytes, 0, newCode.Length);

        caveBytes[newCode.Length] = 0xE9;

        Buffer.BlockCopy(MemoryOperation.ConvertToByteArrayUnsafe(offset), 0, caveBytes, newCode.Length + 1, sizeof(int));

        originalOpcodes = new byte[replaceCount];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, replaceCount, IntPtr.Zero);

        WriteProcessMemory(targetProcessHandle, caveAddress, caveBytes, caveBytes.Length, IntPtr.Zero);
        WriteProcessMemory(targetProcessHandle, targetAddress, jmpBytes, jmpBytes.Length, IntPtr.Zero);

        return true;
    }
}