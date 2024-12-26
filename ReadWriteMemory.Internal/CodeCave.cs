using System.Runtime.CompilerServices;
using ReadWriteMemory.Internal.Entities;
using static ReadWriteMemory.Internal.NativeImports.Kernel32;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    private const byte RelativeCallInstruction = 0xE8;
    private const byte RelativeCallInstructionLength = 5;

    private const byte RelativeJumpInstruction = 0xE9;
    private const byte RelativeJumpInstructionLength = 5;

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
            
            if (!VirtualProtect(targetAddress, table.CodeCaveTable.Value.OriginalOpcodes.Length,
                    PAGE_EXECUTE_READWRITE, out _))
            {
                return CodeCaveTable.Empty;
            }
            
            if (WriteBytes(memoryAddress, table.CodeCaveTable.Value.JmpBytes))
            {
                VirtualProtect(targetAddress, table.CodeCaveTable.Value.OriginalOpcodes.Length,
                    PAGE_EXECUTE_READ, out _);
                
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

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == nuint.Zero)
        {
            return false;
        }

        if (table.CodeCaveTable is null ||
            !VirtualProtect(targetAddress, table.CodeCaveTable.Value.OriginalOpcodes.Length,
                PAGE_EXECUTE_READWRITE, out _))
        {
            return false;
        }

        var writtenBytes = WriteBytes(memoryAddress, table.CodeCaveTable.Value.OriginalOpcodes);

        if (writtenBytes)
        {
            return VirtualProtect(targetAddress, table.CodeCaveTable.Value.OriginalOpcodes.Length,
                PAGE_EXECUTE_READ, out _);
        }
        
        VirtualProtect(targetAddress, table.CodeCaveTable.Value.OriginalOpcodes.Length,
            PAGE_EXECUTE_READ, out _);
        
        return false;
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
            WriteBytes(memoryAddress, table.CodeCaveTable.Value.OriginalOpcodes))
        {
            return false;
        }

        var result = VirtualFree(table.CodeCaveTable.Value.CaveAddress,
            table.CodeCaveTable.Value.SizeOfAllocatedMemory, MemRelease);

        if (!result)
        {
            return false;
        }

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

        var caveAddress = VirtualAlloc(nint.Zero, memoryToAllocate,
            AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);

        if (caveAddress == nuint.Zero)
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
            VirtualFree(caveAddress, memoryToAllocate, MemRelease);

            return CodeCaveTable.Empty;
        }

        var convertedRemainingInstructions =
            ConvertRelativeToAbsoluteInstructions(remainingOpcodes, startOfRemainingOpcodesAddress);

        finalCaveCode.AddRange(convertedRemainingInstructions);

        AppendAbsoluteJumpBackAtTheEndOfSequence(ref finalCaveCode, totalAmountOfOpcodesToReplace, targetAddress);

        if (!WriteBytes(new MemoryAddress(caveAddress), finalCaveCode.ToArray()))
        {
            VirtualFree(caveAddress, memoryToAllocate, MemRelease);

            return CodeCaveTable.Empty;
        }

        var jumpToCaveBytes = GetAbsoluteJumpBytes(caveAddress, totalAmountOfOpcodesToReplace);

        if (!VirtualProtect(targetAddress, jumpToCaveBytes.Length, PAGE_EXECUTE_READWRITE, out _))
        {
            VirtualFree(caveAddress, memoryToAllocate, MemRelease);

            return CodeCaveTable.Empty;
        }

        if (WriteBytes(memoryAddress, jumpToCaveBytes))
        {
            VirtualProtect(targetAddress, jumpToCaveBytes.Length, PAGE_EXECUTE_READ, out _);
            
            var caveTable = new CodeCaveTable(originalOpcodes.ToArray(),
                caveAddress, memoryToAllocate, jumpToCaveBytes);
            
            _memoryRegister[memoryAddress].CodeCaveTable = caveTable;
            
            return caveTable;
        }

        VirtualFree(caveAddress, memoryToAllocate, MemRelease);

        return CodeCaveTable.Empty;
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
                case RelativeCallInstruction:
                {
                    // Example jump: E8 6E C4 85 FF
                    // index of loop: E8
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