using System.Runtime.CompilerServices;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Utilities;
using static ReadWriteMemory.External.NativeImports.Kernel32;

namespace ReadWriteMemory.External;

public sealed partial class RwMemory
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
    /// Creates a code cave to apply custom code in target process. 
    /// If you created a code cave in the past with the same memory address, it will
    /// jump back to your cave address.
    /// </summary>
    /// <param name="memoryAddress">Address, module name and offsets</param>
    /// <param name="caveCode">The opcodes to write in the code cave</param>
    /// <param name="amountOfOpcodesToReplace">The number of bytes of the instruction you want to override/create am jump instruction</param>
    /// <param name="totalAmountOfOpcodesToReplace">Because a x64 jump is 14 bytes large, it will override other instructions, 
    /// so you have to have at least 14 bytes. That means you need to give the next instruction in a whole. For example:
    /// You have a function with 7 bytes, and the next instruction has 4 and the next also 4. That means you have 15. They
    /// get copied in the cave as well since we don't do relative jumps. That's a bit weird, I know, but with that I save
    /// the search of a free space in memory in the near.</param>
    /// <param name="memoryToAllocate">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Cave address</returns>
    public CodeCaveTable CreateOrResumeCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> caveCode,
        int amountOfOpcodesToReplace, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        var table = _memoryRegister.GetOrAdd(memoryAddress, _ => new MemoryAddressTable());

        if (table.CodeCaveTable is null)
        {
            return CreateCodeCave(memoryAddress, caveCode, amountOfOpcodesToReplace, 
                totalAmountOfOpcodesToReplace, memoryToAllocate);
        }
           
        if (!GetTargetAddress(memoryAddress, out var targetAddress))
        {
            CloseCodeCave(memoryAddress);
            return CodeCaveTable.Empty;
        }

        if (MemoryOperation.WriteProcessMemory(_targetProcess.Handle, targetAddress, table.CodeCaveTable.Value.JmpBytes))
        {
            return table.CodeCaveTable.Value;
        }

        CloseCodeCave(memoryAddress);
        
        return CodeCaveTable.Empty;
    }

    /// <summary>
    /// Restores the original opcodes to the memory address without deallocate the memory.
    /// So your code-bytes stay in the memory at the cave address. The advantage is that you
    /// don't have to create a new code cave which costs time. You can simply jump to the cave address
    /// or use the original code. Don't forget to dispose the memory object when you exit the application.
    /// Otherwise, the codecave continue to live forever.
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
    /// Closes a created code cave. Just give this function the memory address where you create a code cave with.
    /// </summary>
    /// <returns>true if the operation was successful, otherwise false.</returns>
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

        _ = DeallocateMemory(table.CodeCaveTable.Value.CaveAddress);

        _memoryRegister[memoryAddress].CodeCaveTable = null;

        return true;
    }

    private CodeCaveTable CreateCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> caveCode,
        int instructionOpcodeLength, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == nuint.Zero || 
            !ReadBytes(memoryAddress, (uint)totalAmountOfOpcodesToReplace, out var originalOpcodes))
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

        if (caveAddress == nuint.Zero || !WriteBytes(new MemoryAddress(caveAddress), finalCaveCode.ToArray()))
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