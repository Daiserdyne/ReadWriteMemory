using System.Runtime.CompilerServices;
using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory
{ 
    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int instructionOpcodes, int totalAmountOfOpcodes,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        jmpBytes = new byte[0];
        originalOpcodes = new byte[0];

        caveAddress = VirtualAllocEx(targetProcessHandle, nuint.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        if (caveAddress == nuint.Zero)
        {
            return false;
        }

        var startAddress = nuint.Add(targetAddress, instructionOpcodes);

        var buffer = new byte[totalAmountOfOpcodes - instructionOpcodes];

        ReadProcessMemory(targetProcessHandle, startAddress, buffer, instructionOpcodes, IntPtr.Zero);

        var tempNewCode = new byte[newCode.Length + buffer.Length];
        Buffer.BlockCopy(newCode, 0, tempNewCode, 0, newCode.Length);

        newCode = tempNewCode;

        for (int i = 0; i < buffer.Length; i++)
        {
            newCode[newCode.Length - 1 - i] = buffer[buffer.Length - 1 - i];
        }

        var jumpBytes = GetJmp64Bytes(caveAddress);
        jmpBytes = jumpBytes;

        var jumpBack = GetJmp64Bytes(nuint.Add(targetAddress, totalAmountOfOpcodes));

        tempNewCode = new byte[newCode.Length + jumpBack.Length];
        Buffer.BlockCopy(newCode, 0, tempNewCode, 0, newCode.Length);

        newCode = tempNewCode;

        for (int i = 0; i < jumpBack.Length; i++)
        {
            newCode[newCode.Length - 1 - i] = jumpBack[jumpBack.Length - 1 - i];
        }

        WriteProcessMemory(targetProcessHandle, caveAddress, newCode, (nuint)newCode.Length, out _);

        WriteJumpToAddress(targetProcessHandle, targetAddress, (nuint)totalAmountOfOpcodes, jumpBytes, out originalOpcodes);

        return true;
    }

    private static ReadOnlySpan<byte> JumpAsm => new byte[]
    {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,                // jmp qword ptr [$+6]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00     // ptr
    };

    private static byte[] GetJmp64Bytes(nuint caveAddress)
    {
        var jumpBytes = new byte[14];

        JumpAsm.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], caveAddress);

        jumpBytes.AsSpan(14).Fill(0x90);

        return jumpBytes;
    }

    private static void WriteJumpToAddress(nint targetProcessHandle, nuint targetAddress, nuint replaceCount, byte[] jumpBytes, out byte[] originalOpcodes)
    {
        originalOpcodes = new byte[replaceCount];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, (int)replaceCount, IntPtr.Zero);

        WriteProcessMemory(targetProcessHandle, targetAddress, jumpBytes, replaceCount, out _);
    }
}