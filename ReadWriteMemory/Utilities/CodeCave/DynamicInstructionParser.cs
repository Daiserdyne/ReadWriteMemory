using System.Runtime.CompilerServices;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class DynamicInstructionParser
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

    internal static byte[] GetConvertedInstructions(byte[] newCode, byte[] totalOpcodes, int instructionOpcodesLength, nuint targetAddress)
    {
        var convertedCode = new List<byte>(newCode);

        var callIndex = 0;
        var calls = GetAllx86CallIndices(totalOpcodes, instructionOpcodesLength);

        var jumpIndex = 0;
        var jumps = GetAllx86JumpIndices(totalOpcodes, instructionOpcodesLength);

        for (int index = 0; index < newCode.Length; index++)
        {
            switch (newCode[index])
            {
                case X86CallInstruction:
                    {
                        ConvertX86ToX64Call(ref convertedCode, index, calls, callIndex, targetAddress);

                        break;
                    }

                case X86JumpInstruction:
                    {

                        break;
                    }

                default:
                    break;
            }
        }

        return convertedCode.ToArray();
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

    private static List<int> GetAllx86CallIndices(byte[] totalOpcodes, int instructionOpcodesLength)
    {
        return GetIndicesOfInstruction(totalOpcodes, instructionOpcodesLength, X86CallInstruction);
    }

    private static List<int> GetAllx86JumpIndices(byte[] totalOpcodes, int instructionOpcodesLength)
    {
        return GetIndicesOfInstruction(totalOpcodes, instructionOpcodesLength, X86JumpInstruction);
    }

    private static List<int> GetIndicesOfInstruction(byte[] totalOpcodes, int instructionOpcodesLength, int instruction)
    {
        var instructionIndices = new List<int>();

        for (int i = 0; i < totalOpcodes.Length; i++)
        {
            if (totalOpcodes[i] == instruction)
            {
                instructionIndices.Add(i + instructionOpcodesLength);
            }
        }

        return instructionIndices;
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
}