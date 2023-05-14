using System.Runtime.CompilerServices;
using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory
{
    private const byte CallInstruction = 0xE8;

    private static ReadOnlySpan<byte> _jumpAsmTemplate => new byte[]
    {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,                // jmp qword ptr [$+6]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00     // ptr
    };

    private static ReadOnlySpan<byte> _callAsmTemplate => new byte[]
    {
        0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0x08,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

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

        newCode = ParseNewCodeBytes(newCode, buffer, instructionOpcodes, targetAddress);

        var jumpBytes = GetJmp64Bytes(caveAddress, totalAmountOfOpcodes);
        jmpBytes = jumpBytes;

        var jumpBack = GetJmp64Bytes(nuint.Add(targetAddress, totalAmountOfOpcodes), totalAmountOfOpcodes);

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

    private static byte[] ParseNewCodeBytes(byte[] newCode, byte[] buffer, int instructionOpcodesLength, nuint targetAddress)
    {
        var parsedCode = new List<byte>(newCode);

        ushort callIndex = 0;

        var calls = new List<int>();

        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == CallInstruction)
            {
                calls.Add(i + instructionOpcodesLength);
            }
        }

        for (int i = 0; i < newCode.Length; i++)
        {
            switch (newCode[i])
            {
                case CallInstruction:
                    {
                        var x86Call = new byte[5];

                        var counter = i;

                        for (ushort j = 0; j < 5; j++)
                        {
                            x86Call[j] = newCode[counter++];
                        }

                        parsedCode.RemoveRange(i, 5);
                        parsedCode.InsertRange(i, ConvertX86ToX64Call(x86Call, calls[callIndex++], targetAddress));

                        break;
                    }

                default:
                    break;
            }
        }

        return parsedCode.ToArray();
    }

    private static byte[] ConvertX86ToX64Call(byte[] x86Call, int index, nuint targetAddress)
    {
        Array.Reverse(x86Call);

        x86Call = new byte[] { x86Call[3], x86Call[2], x86Call[1], x86Call[0] };

        var relativeAddress = BitConverter.ToInt32(x86Call) + 5;

        var callAddress = nuint.Add(targetAddress, index);
        var finalAddress = nuint.Add(callAddress, relativeAddress);

        var x64Call = new byte[16];

        _callAsmTemplate.CopyTo(x64Call);

        Unsafe.WriteUnaligned(ref x64Call[8], finalAddress);

        return x64Call;
    }

    private static byte[] GetJmp64Bytes(nuint caveAddress, int replaceCount)
    {
        if (replaceCount < 14)
        {
            throw new Exception("Replace count is to small, must be 14 bytes min.");
        }

        var jumpBytes = new byte[replaceCount];

        _jumpAsmTemplate.CopyTo(jumpBytes);

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