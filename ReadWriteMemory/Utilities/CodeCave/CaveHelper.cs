using System.Runtime.CompilerServices;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class CaveHelper
{
    private const byte RelativeCallInstruction = 0xE8;
    private const ushort RelativeCallInstructionLength = 5;

    private const byte RelativeJumpInstruction = 0xE9;
    private const ushort RelativeJumpInstructionLength = 5;

    private static ReadOnlySpan<byte> _jumpAsmTemplate =>
    [
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    private static ReadOnlySpan<byte> _callAsmTemplate =>
    [
        0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0x08,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    private static byte[] ConvertToAbsoluteCall(byte[] relativeCall, nuint targetAddress, int offset)
    {
        Array.Reverse(relativeCall);

        relativeCall = [relativeCall[3], relativeCall[2], relativeCall[1], relativeCall[0]];

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

        relativeJump = [relativeJump[3], relativeJump[2], relativeJump[1], relativeJump[0]];

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

    internal static int AppendJumpBack(ref List<byte> caveCode, nuint jumpBackAddress)
    {
        var jumpIndices = new List<int>();

        for (var index = 0; index < caveCode.Count; index++)
        {
            if (caveCode[index] == RelativeJumpInstruction)
            {
                jumpIndices.Add(index);
            }
        }

        var absoulteJump = GetAbsoluteJumpBytes(jumpBackAddress, _jumpAsmTemplate.Length);

        if (!jumpIndices.Any())
        {
            caveCode.AddRange(absoulteJump);

            return caveCode.Count - _jumpAsmTemplate.Length;
        }

        var targetJumpIndex = jumpIndices.LastOrDefault();

        if (targetJumpIndex == default || targetJumpIndex + 5 > caveCode.Count)
        {
            return default;
        }

        caveCode.RemoveRange(targetJumpIndex, RelativeJumpInstructionLength);
        caveCode.InsertRange(targetJumpIndex, absoulteJump);

        return targetJumpIndex;
    }

    internal static byte[] GetAbsoluteJumpBytes(nuint jumpToAddress, int opcodesToReplace, bool nopRestOpcodes = false)
    {
        var jumpBytes = new byte[nopRestOpcodes ? opcodesToReplace : _jumpAsmTemplate.Length];

        _jumpAsmTemplate.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], jumpToAddress);

        jumpBytes.AsSpan(14).Fill(0x90);

        return jumpBytes;
    }
}
