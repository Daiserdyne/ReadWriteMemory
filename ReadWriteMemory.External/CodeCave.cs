using System.Runtime.CompilerServices;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Utilities;
using static ReadWriteMemory.External.NativeImports.Kernel32;

namespace ReadWriteMemory.External;

public partial class RwMemory
{
    private const byte RelativeCallInstruction = 0xE8;
    private const byte RelativeCallInstructionLength = 5;

    private const byte RelativeJumpInstruction = 0xE9;
    private const byte RelativeJumpInstructionLength = 5;

    private const byte RelativeShortJumpInstruction = 0xEB;
    private const byte RelativeShortJumpInstructionLength = 2;

    private static ReadOnlySpan<byte> JumpAsmTemplate =>
    [
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    private static ReadOnlySpan<byte> CallAsmTemplate =>
    [
        0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0x08,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="caveCode"></param>
    /// <param name="amountOfOpcodesToReplace"></param>
    /// <param name="totalAmountOfOpcodesToReplace"></param>
    /// <param name="memoryToAllocate"></param>
    /// <returns></returns>
    public CodeCaveTable CreateOrResumeCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> caveCode,
        int amountOfOpcodesToReplace, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (table.CodeCaveTable is not null)
        {
            if (!GetTargetAddress(memoryAddress, out var targetAddress))
            {
                CloseCodeCave(memoryAddress);

                return CodeCaveTable.Empty;
            }

            if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress,
                    table.CodeCaveTable.Value.JmpBytes))
            {
                return table.CodeCaveTable.Value;
            }

            CloseCodeCave(memoryAddress);

            return CodeCaveTable.Empty;
        }

        return CreateCodeCave(memoryAddress, caveCode, amountOfOpcodesToReplace,
            totalAmountOfOpcodesToReplace, memoryToAllocate);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool PauseOpenedCodeCave(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            return false;
        }

        return table.CodeCaveTable is not null &&
               WriteBytes(memoryAddress, table.CodeCaveTable.Value.OriginalOpcodes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <returns></returns>
    public bool CloseCodeCave(MemoryAddress memoryAddress)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            return false;
        }

        if (table.CodeCaveTable is null ||
            !WriteBytes(memoryAddress, table.CodeCaveTable.Value.OriginalOpcodes))
        {
            return false;
        }

        DeallocateMemory(table.CodeCaveTable.Value.CaveAddress);

        _memoryRegister[memoryAddress].CodeCaveTable = null;

        return true;
    }

    private CodeCaveTable CreateCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> caveCode,
        int instructionOpcodeLength, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == nuint.Zero)
        {
            return CodeCaveTable.Empty;
        }

        if (!ReadBytes(memoryAddress, (uint)totalAmountOfOpcodesToReplace, out var originalOpcodes))
        {
            return CodeCaveTable.Empty;
        }

        var finalCaveCode = new List<byte>(caveCode.ToArray());

        if (finalCaveCode[^RelativeJumpInstructionLength] == RelativeJumpInstruction)
        {
            // This is to remove the relative jump back.
            RemoveLastRelativeJumpInSequence(ref finalCaveCode);
        }

        var startOfRemainingOpcodesAddress = nuint.Add(targetAddress, instructionOpcodeLength);

        var remainingOpcodesLength = totalAmountOfOpcodesToReplace - instructionOpcodeLength;

        if (!ReadBytes(new MemoryAddress(startOfRemainingOpcodesAddress), (uint)remainingOpcodesLength,
                out var remainingOpcodes))
        {
            return CodeCaveTable.Empty;
        }

        var convertedRemainingInstructions =
            ConvertRelativeToAbsoluteInstructions(remainingOpcodes, startOfRemainingOpcodesAddress);

        finalCaveCode.AddRange(convertedRemainingInstructions);

        AppendAbsoluteJumpBackAtTheEndOfSequence(ref finalCaveCode, totalAmountOfOpcodesToReplace, targetAddress);

        var caveAddress = VirtualAllocEx(_targetProcess.Handle, nuint.Zero, memoryToAllocate,
            MemCommit | MemReserve, PageExecuteReadwrite);

        if (caveAddress == nuint.Zero)
        {
            return CodeCaveTable.Empty;
        }

        if (!WriteBytes(new MemoryAddress(caveAddress), finalCaveCode.ToArray()))
        {
            return CodeCaveTable.Empty;
        }

        var jumpToCaveBytes = GetAbsoluteJumpBytes(caveAddress, totalAmountOfOpcodesToReplace);

        if (!WriteBytes(memoryAddress, jumpToCaveBytes))
        {
            return CodeCaveTable.Empty;
        }

        var caveTable = new CodeCaveTable(originalOpcodes.ToArray(),
            caveAddress, memoryToAllocate, jumpToCaveBytes);

        _memoryRegister[memoryAddress].CodeCaveTable = caveTable;

        return caveTable;
    }

    private static unsafe List<byte> ConvertRelativeToAbsoluteInstructions(ReadOnlySpan<byte> remainingInstructions,
        nuint startOfRemainingOpcodesAddress)
    {
        var newCustomCode = new List<byte>();

        for (var index = 0; index < remainingInstructions.Length; index++)
        {
            if (index + RelativeJumpInstructionLength > remainingInstructions.Length)
            {
                newCustomCode.Add(remainingInstructions[index]);
                continue;
            }

            switch (remainingInstructions[index])
            {
                case RelativeJumpInstruction:
                {
                    // Example jump: E9 6E C4 85 FF
                    // index of loop: E9
                    // convert to little indian format
                    byte[] relativeAddressOffsetBytes =
                    [
                        remainingInstructions[index + 4], // FF
                        remainingInstructions[index + 3], // 85
                        remainingInstructions[index + 2], // C4
                        remainingInstructions[index + 1] // 6E
                    ];

                    int relativeAddressOffset;

                    fixed (byte* offsetAsPtr = relativeAddressOffsetBytes)
                    {
                        relativeAddressOffset = *(int*)offsetAsPtr;
                    }

                    // Goes to start of jump (E9)
                    var callerAddress = nuint.Add(startOfRemainingOpcodesAddress, index);

                    // Adds size of the jump to the address.
                    var relativeAddress = callerAddress + RelativeJumpInstructionLength;

                    // Calculates the jump address.
                    var jumpAddress = nuint.Add(relativeAddress, relativeAddressOffset);

                    var absoluteJumpBytes = GetAbsoluteJumpBytes(jumpAddress);

                    newCustomCode.AddRange(absoluteJumpBytes);

                    index += RelativeJumpInstructionLength - 1;

                    break;
                }
                case RelativeShortJumpInstruction:
                {
                    // Example jump: EB 65 
                    // index of loop: EB
                    // convert to little indian format
                    byte[] relativeAddressOffsetBytes =
                    [
                        remainingInstructions[index + 1] // 65
                    ];

                    int relativeAddressOffset;

                    fixed (byte* offsetAsPtr = relativeAddressOffsetBytes)
                    {
                        relativeAddressOffset = *(int*)offsetAsPtr;
                    }

                    // Goes to start of jump (E9)
                    var callerAddress = nuint.Add(startOfRemainingOpcodesAddress, index);

                    // Adds size of the jump to the address.
                    var relativeAddress = callerAddress + RelativeShortJumpInstructionLength;

                    // Calculates the jump address.
                    var jumpAddress = nuint.Add(relativeAddress, relativeAddressOffset);

                    var absoluteJumpBytes = GetAbsoluteJumpBytes(jumpAddress);

                    newCustomCode.AddRange(absoluteJumpBytes);

                    index += RelativeShortJumpInstructionLength - 1;

                    break;
                }
                case RelativeCallInstruction:
                {
                    byte[] relativeAddressOffsetBytes =
                    [
                        remainingInstructions[index + 4], // FF
                        remainingInstructions[index + 3], // 85 
                        remainingInstructions[index + 2], // C4
                        remainingInstructions[index + 1] // 6E
                    ];

                    int relativeAddressOffset;

                    fixed (byte* offsetAsPtr = relativeAddressOffsetBytes)
                    {
                        relativeAddressOffset = *(int*)offsetAsPtr;
                    }

                    // Goes to start of jump (E8)
                    var callerAddress = nuint.Add(startOfRemainingOpcodesAddress, index);

                    // Adds size of the jump to the address.
                    var relativeAddress = callerAddress + RelativeCallInstructionLength;

                    // Calculates the jump address.
                    var callAddress = nuint.Add(relativeAddress, relativeAddressOffset);

                    var absoluteJumpBytes = GetAbsoluteCallBytes(callAddress);

                    newCustomCode.AddRange(absoluteJumpBytes);

                    index += RelativeCallInstructionLength - 1;

                    break;
                }
                default:
                {
                    newCustomCode.Add(remainingInstructions[index]);
                    break;
                }
            }
        }

        return newCustomCode;
    }

    private static void RemoveLastRelativeJumpInSequence(ref List<byte> customCode)
    {
        customCode = customCode[..^RelativeJumpInstructionLength];
    }

    private static void AppendAbsoluteJumpBackAtTheEndOfSequence(ref List<byte> customCode,
        int totalAmountOfOpcodesToReplace, nuint targetAddress)
    {
        var jumpBackAddress = nuint.Add(targetAddress, totalAmountOfOpcodesToReplace);

        var jumpBackBytes = GetAbsoluteJumpBytes(jumpBackAddress);

        customCode.AddRange(jumpBackBytes);
    }

    private static byte[] GetAbsoluteJumpBytes(nuint jumpToAddress, int opcodesToReplace = 0)
    {
        var length = Math.Max(JumpAsmTemplate.Length, opcodesToReplace);

        var jumpBytes = new byte[length];

        JumpAsmTemplate.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], jumpToAddress);

        if (length > JumpAsmTemplate.Length)
        {
            jumpBytes.AsSpan(JumpAsmTemplate.Length).Fill(0x90);
        }

        return jumpBytes;
    }

    private static byte[] GetAbsoluteCallBytes(nuint callAddress)
    {
        var callBytes = new byte[CallAsmTemplate.Length];

        CallAsmTemplate.CopyTo(callBytes);

        Unsafe.WriteUnaligned(ref callBytes[8], callAddress);

        return callBytes;
    }
}