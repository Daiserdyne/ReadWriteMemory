using System.Runtime.CompilerServices;
using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory2
{
    // 
    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int replaceCount,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        caveAddress = VirtualAllocEx(targetProcessHandle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        var buffer = new byte[replaceCount];

        ReadProcessMemory(targetProcessHandle, targetAddress, buffer, replaceCount, IntPtr.Zero);

        Array.Copy(buffer, newCode, 0);

        WriteProcessMemory(targetProcessHandle, caveAddress, newCode, (nuint)newCode.Length, out _);

        MakeJmp64(targetProcessHandle, targetAddress, caveAddress, out jmpBytes, out originalOpcodes, (nuint)replaceCount);

        return true;
    }

    private static ReadOnlySpan<byte> JumpAsm => new byte[]
    {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,                // jmp qword ptr [$+6]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00     // ptr
    };

    private static void MakeJmp64(IntPtr targetProcessHandle, UIntPtr targetAddress, UIntPtr caveAddress, out byte[] write, out byte[] originalOpcodes, UIntPtr dwLen = 14)
    {
        originalOpcodes = new byte[dwLen];
        write = new byte[0];

        if (dwLen < 14)
        {
            return;
        }

        write = new byte[dwLen];

        JumpAsm.CopyTo(write);

        Unsafe.WriteUnaligned(ref write[6], caveAddress);

        write.AsSpan((int)dwLen).Fill(0x90);

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, (int)dwLen, IntPtr.Zero);

        VirtualProtectEx(targetProcessHandle, targetAddress, dwLen, MemoryProtection.ExecuteReadWrite, out _);

        WriteProcessMemory(targetProcessHandle, targetAddress, write, dwLen, out _);

        VirtualProtectEx(targetProcessHandle, targetAddress, dwLen, 0x0, out _);
    }
}