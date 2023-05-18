using System.Runtime.CompilerServices;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class CaveHelper
{
    private const byte RelativeCallInstruction = 0xE8;
    private const ushort RelativeCallInstructionLength = 5;

    private const byte RelativeJumpInstruction = 0xE9;
    private const ushort RelativeJumpInstructionLength = 5;

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

    private static byte[] ConvertToAbsoluteCall(byte[] relativeCall, nuint targetAddress, int offset)
    {
        Array.Reverse(relativeCall);

        relativeCall = new byte[] { relativeCall[3], relativeCall[2], relativeCall[1], relativeCall[0] };

        MemoryOperation.ConvertBufferUnsafe<int>(relativeCall, out var value);

        var callerAddress = nuint.Add(targetAddress, offset);
        var relativeAddress = value + RelativeCallInstructionLength;
        var funcAddress = nuint.Add(callerAddress, relativeAddress);

        var absoluteCall = new byte[_callAsmTemplate.Length];

        _callAsmTemplate.CopyTo(absoluteCall);

        Unsafe.WriteUnaligned(ref absoluteCall[8], funcAddress);

        return absoluteCall;
    }

    private static byte[] ConvertToAbsoluteJump(byte[] relativeJump, nuint targetAddress, int offset)
    {
        Array.Reverse(relativeJump);

        relativeJump = new byte[] { relativeJump[3], relativeJump[2], relativeJump[1], relativeJump[0] };

        MemoryOperation.ConvertBufferUnsafe<int>(relativeJump, out var value);

        var callerAddress = nuint.Add(targetAddress, offset);
        var relativeAddress = value + RelativeJumpInstructionLength;
        var jumpAddress = nuint.Add(callerAddress, relativeAddress);

        var absoluteJump = new byte[_jumpAsmTemplate.Length];

        _jumpAsmTemplate.CopyTo(absoluteJump);

        Unsafe.WriteUnaligned(ref absoluteJump[6], jumpAddress);

        return absoluteJump;
    }

    internal static List<byte> ConvertRemainingInstructions(byte[] remainingInstructions, nuint targetAddress)
    {
        var convertedInstructions = new List<byte>(remainingInstructions);

        for (var index = 0; index < remainingInstructions.Length; index++)
        {
            switch (remainingInstructions[index])
            {
                case RelativeCallInstruction:
                    {
                        if (index + RelativeCallInstructionLength <= remainingInstructions.Length)
                        {
                            var absoluteCall = ConvertToAbsoluteCall(remainingInstructions.Take(RelativeCallInstructionLength).ToArray(), 
                                targetAddress, index);

                            convertedInstructions.RemoveRange(index, RelativeCallInstructionLength);
                            convertedInstructions.InsertRange(index, absoluteCall);

                            index += RelativeCallInstructionLength - 1;
                        }

                        break;
                    }

                case RelativeJumpInstruction:
                    {
                        if (index + RelativeCallInstructionLength <= remainingInstructions.Length)
                        {
                            var absoluteCall = ConvertToAbsoluteJump(remainingInstructions.Take(RelativeCallInstructionLength).ToArray(),
                                targetAddress, index);

                            convertedInstructions.RemoveRange(index, RelativeCallInstructionLength);
                            convertedInstructions.InsertRange(index, absoluteCall);

                            index += RelativeCallInstructionLength - 1;
                        }

                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }

        return convertedInstructions;
    }

    //internal static List<byte> ConvertAllX86ToX64Calls(List<byte> newCode, List<byte> totalOpcodes, int instructionOpcodesLength, nuint targetAddress)
    //{
    //    var calls = GetAllx86CallIndices(totalOpcodes, instructionOpcodesLength);

    //    if (!calls.Any())
    //    {
    //        return newCode;
    //    }

    //    var convertedCode = new List<byte>(newCode);

    //    var callIndex = 0;

    //    for (int index = 0; index < newCode.Count; index++)
    //    {
    //        switch (newCode[index])
    //        {
    //            case RelativeCallInstruction:
    //                {
    //                    ConvertX86ToX64Call(ref convertedCode, index, calls, callIndex, targetAddress);

    //                    break;
    //                }

    //            case RelativeJumpInstruction:
    //                {

    //                    break;
    //                }

    //            default:
    //                break;
    //        }
    //    }

    //    return convertedCode;
    //}

    //internal static void ConvertRelativeToAbsoluteJumpBack(ref List<byte> code, nuint targetAddress, int instructionOpcodesLength, int totalAmountOfOpcodes)
    //{
    //    var jumpIndex = GetAllX86JumpIndices(code, instructionOpcodesLength).LastOrDefault();

    //    if (jumpIndex == 0 || code.Count - 6 != jumpIndex)
    //    {
    //        code.InsertRange(code.Count, GetX64JumpBytes(nuint.Add(targetAddress, totalAmountOfOpcodes), _jumpAsmTemplate.Length, true));
    //        return;
    //    }

    //    code.RemoveRange(jumpIndex, 5);
    //    code.InsertRange(jumpIndex, GetX64JumpBytes(targetAddress, _jumpAsmTemplate.Length, true));
    //}

    //private static void ConvertX86ToX64Call(ref List<byte> newCode, int index, List<int> calls, int callIndex, nuint targetAddress)
    //{
    //    var x86Call = new byte[5];

    //    var counter = index;

    //    for (ushort j = 0; j < 5; j++)
    //    {
    //        x86Call[j] = newCode[counter++];
    //    }

    //    newCode.RemoveRange(index, 5);
    //    newCode.InsertRange(index, ConvertX86ToX64Call(x86Call, calls[callIndex++], targetAddress));
    //}

    //private static List<int> GetAllx86CallIndices(List<byte> totalOpcodes, int instructionOpcodesLength)
    //{
    //    return GetIndicesOfInstruction(totalOpcodes, instructionOpcodesLength, RelativeCallInstruction);
    //}

    //private static List<int> GetAllX86JumpIndices(List<byte> totalOpcodes, int instructionOpcodesLength)
    //{
    //    return GetIndicesOfInstruction(totalOpcodes, instructionOpcodesLength, RelativeJumpInstruction);
    //}

    //private static List<int> GetIndicesOfInstruction(List<byte> totalOpcodes, int instructionOpcodeLength, byte searchedInstruction)
    //{
    //    var instructionIndices = new List<int>();

    //    for (int i = 0; i < totalOpcodes.Count; i++)
    //    {
    //        if (totalOpcodes[i] == searchedInstruction)
    //        {
    //            instructionIndices.Add(i + instructionOpcodeLength);
    //        }
    //    }

    //    return instructionIndices;
    //}

    //private static byte[] ConvertX86ToX64Call(byte[] x86Call, int offset, nuint targetAddress)
    //{
    //    Array.Reverse(x86Call);

    //    x86Call = new byte[] { x86Call[3], x86Call[2], x86Call[1], x86Call[0] };

    //    MemoryOperation.ConvertBufferUnsafe<int>(x86Call, out var value);

    //    var callerAddress = nuint.Add(targetAddress, offset);
    //    var relativeAddress = value + 5;
    //    var funcAddress = nuint.Add(callerAddress, relativeAddress);

    //    var x64Call = new byte[_callAsmTemplate.Length];

    //    _callAsmTemplate.CopyTo(x64Call);

    //    Unsafe.WriteUnaligned(ref x64Call[8], funcAddress);

    //    return x64Call;
    //}

    internal static byte[] GetX64JumpBytes(nuint jumpToAddress, int opcodesToReplace, bool nopRestOpcodes = false)
    {
        var jumpBytes = new byte[nopRestOpcodes ? opcodesToReplace : _jumpAsmTemplate.Length];

        _jumpAsmTemplate.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], jumpToAddress);

        jumpBytes.AsSpan(14).Fill(0x90);

        return jumpBytes;
    }
}