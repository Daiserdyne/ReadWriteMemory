using System.Runtime.CompilerServices;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class CaveHelper
{
    private const byte X86CallInstruction = 0xE8;
    private const byte X86JumpInstruction = 0xE9;

    private static ReadOnlySpan<byte> _jumpAsmTemplate => new byte[]
    {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    private static ReadOnlySpan<byte> _callAsmTemplate => new byte[]
    {
        0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0x08,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    internal static List<byte> ConvertAllX86ToX64Calls(List<byte> newCode, List<byte> totalOpcodes, int instructionOpcodesLength, nuint targetAddress)
    {
        var calls = GetAllx86CallIndices(totalOpcodes, instructionOpcodesLength);    

        if (!calls.Any())
        {
            return newCode;
        }

        var convertedCode = new List<byte>(newCode);

        var callIndex = 0;

        for (int index = 0; index < newCode.Count; index++)
        {
            switch (newCode[index])
            {
                case X86CallInstruction:
                    {
                        ConvertX86ToX64Call(ref convertedCode, index, calls, callIndex, targetAddress);

                        break;
                    }

                default:
                    break;
            }
        }

        return convertedCode;
    }

    internal static void ConvertX86ToX64JumpBack(ref List<byte> code, nuint targetAddress, int instructionOpcodesLength)
    {
        var jumpIndex = GetAllX86JumpIndices(code, instructionOpcodesLength).LastOrDefault();

        if (jumpIndex == 0 || code[jumpIndex + 5] != 0x00)
        {
            return; 
        }

        code.RemoveRange(jumpIndex, 5);
        code.InsertRange(jumpIndex, GetX64JumpBytes(targetAddress, _jumpAsmTemplate.Length, true));
    }

    private static void ConvertX86ToX64Call(ref List<byte> newCode, int index, List<int> calls, int callIndex, nuint targetAddress)
    {
        var x86Call = new byte[5];

        var counter = index;

        for (ushort j = 0; j < 5; j++)
        {
            x86Call[j] = newCode[counter++];
        }

        newCode.RemoveRange(index, 5);
        newCode.InsertRange(index, ConvertX86ToX64Call(x86Call, calls[callIndex++], targetAddress));
    }

    private static List<int> GetAllx86CallIndices(List<byte> totalOpcodes, int instructionOpcodesLength)
    {
        return GetIndicesOfInstruction(totalOpcodes, instructionOpcodesLength, X86CallInstruction);
    }

    private static List<int> GetAllX86JumpIndices(List<byte> totalOpcodes, int instructionOpcodesLength)
    {
        return GetIndicesOfInstruction(totalOpcodes, instructionOpcodesLength, X86JumpInstruction);
    }

    private static List<int> GetIndicesOfInstruction(List<byte> totalOpcodes, int instructionOpcodeLength, byte searchedInstruction)
    {
        var instructionIndices = new List<int>();

        for (int i = 0; i < totalOpcodes.Count; i++)
        {
            if (totalOpcodes[i] == searchedInstruction)
            {
                instructionIndices.Add(i + instructionOpcodeLength);
            }
        }

        return instructionIndices;
    }

    private static byte[] ConvertX86ToX64Call(byte[] x86Call, int offset, nuint targetAddress)
    {
        Array.Reverse(x86Call);

        x86Call = new byte[] { x86Call[3], x86Call[2], x86Call[1], x86Call[0] };

        MemoryOperation.ConvertBufferUnsafe<int>(x86Call, out var value);

        var callerAddress = nuint.Add(targetAddress, offset);
        var relativeAddress = value + 5;
        var funcAddress = nuint.Add(callerAddress, relativeAddress);

        var x64Call = new byte[_callAsmTemplate.Length];

        _callAsmTemplate.CopyTo(x64Call);

        Unsafe.WriteUnaligned(ref x64Call[8], funcAddress);

        return x64Call;
    }

    internal static byte[] GetX64JumpBytes(nuint jumpToAddress, int opcodesToReplace, bool isJumpBack = false)
    {
        var jumpBytes = new byte[isJumpBack ? _jumpAsmTemplate.Length : opcodesToReplace];

        _jumpAsmTemplate.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], jumpToAddress);

        jumpBytes.AsSpan(14).Fill(0x90);

        return jumpBytes;
    }
}