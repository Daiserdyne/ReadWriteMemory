using System.Runtime.CompilerServices;
using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory2
{ 
    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int targetAddressOpcodeLength, int opcodesToReplace,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        caveAddress = VirtualAllocEx(targetProcessHandle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        var startAddress = nuint.Add(targetAddress, targetAddressOpcodeLength);

        var buffer = new byte[opcodesToReplace - targetAddressOpcodeLength];

        ReadProcessMemory(targetProcessHandle, startAddress, buffer, targetAddressOpcodeLength, IntPtr.Zero);

        var tempNewCode = new byte[newCode.Length + buffer.Length];
        Buffer.BlockCopy(newCode, 0, tempNewCode, 0, newCode.Length);

        newCode = tempNewCode;

        for (int i = 0; i < buffer.Length; i++)
        {
            newCode[newCode.Length - 1 - i] = buffer[buffer.Length - 1 - i];
        }

        var jumpBytes = GetJmp64Bytes(targetProcessHandle, targetAddress, caveAddress);
        jmpBytes = jumpBytes;

        var tempCave = nuint.Add(caveAddress, newCode.Length);

        var jumpBack = GetJmp64Bytes(targetProcessHandle, tempCave, nuint.Add(targetAddress, opcodesToReplace));

        tempNewCode = new byte[newCode.Length + jumpBack.Length];
        Buffer.BlockCopy(newCode, 0, tempNewCode, 0, newCode.Length);

        newCode = tempNewCode;

        for (int i = 0; i < jumpBack.Length; i++)
        {
            newCode[newCode.Length - 1 - i] = jumpBack[jumpBack.Length - 1 - i];
        }

        WriteProcessMemory(targetProcessHandle, caveAddress, newCode, (nuint)newCode.Length, out _);

        WriteJumpToAddress(targetProcessHandle, targetAddress, caveAddress, (nuint)opcodesToReplace, jumpBytes, out originalOpcodes);

        return true;
    }

    private static ReadOnlySpan<byte> JumpAsm => new byte[]
    {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,                // jmp qword ptr [$+6]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00     // ptr
    };

    private static byte[] GetJmp64Bytes(IntPtr targetProcessHandle, UIntPtr targetAddress, UIntPtr caveAddress)
    {
        var jumpBytes = new byte[14];

        JumpAsm.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], caveAddress);

        jumpBytes.AsSpan(14).Fill(0x90);

        return jumpBytes;
    }

    private static void WriteJumpToAddress(IntPtr targetProcessHandle, UIntPtr targetAddress, UIntPtr caveAddress, nuint replaceCount, byte[] jumpBytes, out byte[] originalOpcodes)
    {
        originalOpcodes = new byte[replaceCount];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, (int)replaceCount, IntPtr.Zero);

        VirtualProtectEx(targetProcessHandle, targetAddress, replaceCount, MemoryProtection.ExecuteReadWrite, out _);

        WriteProcessMemory(targetProcessHandle, targetAddress, jumpBytes, replaceCount, out _);

        VirtualProtectEx(targetProcessHandle, targetAddress, replaceCount, 0x0, out _);
    }
}