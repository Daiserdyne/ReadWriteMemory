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
    /// <param name="customCode"></param>
    /// <param name="amountOfOpcodesToReplace"></param>
    /// <param name="totalAmountOfOpcodesToReplace"></param>
    /// <param name="memoryToAllocate"></param>
    /// <returns></returns>
    public async ValueTask<nuint> CreateOrResumeCodeCave(MemoryAddress memoryAddress, byte[] customCode,
        int amountOfOpcodesToReplace, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        if (!_memoryRegister.TryGetValue(memoryAddress, out var table))
        {
            _memoryRegister.Add(memoryAddress, new MemoryAddressTable());
        }
        else if (table.CodeCaveTable is not null)
        {
            if (!WriteBytes(memoryAddress, table.CodeCaveTable.Value.JmpBytes))
            {
                // ignored for now.
            }
            
            return table.CodeCaveTable.Value.CaveAddress;
        }

        return await Task.Run(() => CreateCodeCave(memoryAddress, customCode,
            amountOfOpcodesToReplace, totalAmountOfOpcodesToReplace, memoryToAllocate));
    }

    private nuint CreateCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> customCode,
        int instructionOpcodeLength, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == nuint.Zero)
        {
            return nuint.Zero;
        }

        if (!ReadBytes(memoryAddress, (uint)totalAmountOfOpcodesToReplace, out var originalOpcodes))
        {
            return nuint.Zero;
        }

        var caveAddress = VirtualAlloc(nint.Zero, memoryToAllocate,
            AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);

        if (caveAddress == nuint.Zero)
        {
            return nuint.Zero;
        }

        if (customCode[^RelativeJumpInstructionLength] == RelativeJumpInstruction)
        {
            RemoveLastRelativeJumpInSequence(ref customCode);
        }

        AppendAbsoluteJumpBackAtTheEndOfSequence(ref customCode, totalAmountOfOpcodesToReplace, targetAddress);
        
        var startOfRemainingOpcodesAddress = nuint.Add(targetAddress, instructionOpcodeLength);

        var remainingOpcodesAddress = new MemoryAddress(memoryAddress.ModuleName, 
            startOfRemainingOpcodesAddress, memoryAddress.Offsets);
        
        var remainingOpcodesLength = totalAmountOfOpcodesToReplace - instructionOpcodeLength;
        
        if (!ReadBytes(remainingOpcodesAddress, (uint)remainingOpcodesLength, 
                out var remainingInstructions))
        {
            VirtualFree(caveAddress, memoryToAllocate, MemRelease);
            
            return nuint.Zero;
        }
        
        ConvertRemainingOpcodesToAbsoluteInstructions(ref remainingInstructions, startOfRemainingOpcodesAddress);

        var jumpToCaveBytes = GetAbsoluteJumpBytes(caveAddress, totalAmountOfOpcodesToReplace);


        return nuint.Zero;
    }

    private static byte[] ConvertRemainingOpcodesToAbsoluteInstructions(ReadOnlySpan<byte> customCode, 
        nuint startOfRemainingOpcodes)
    {
        var newCustomCode = new List<byte>();

        var index = 0;

        while (index < customCode.Length)
        {
            if (customCode[index] == RelativeJumpInstruction && index + RelativeJumpInstructionLength <= customCode.Length)
            {
                var offset = BitConverter.ToInt32(customCode.Slice(index + 1, 4));
                
                var absoluteAddress = (nuint)((int)startOfRemainingOpcodes + index + RelativeJumpInstructionLength + offset);
                
                var absoluteJumpBytes = GetAbsoluteJumpBytes(absoluteAddress);
                newCustomCode.AddRange(absoluteJumpBytes);

                index += RelativeJumpInstructionLength;
            }
            else
            {
                newCustomCode.Add(customCode[index]);
                index++;
            }
        }

        return newCustomCode.ToArray();
    }
    
    private static void ConvertRemainingOpcodesToAbsoluteInstructions(
        ref ReadOnlySpan<byte> remainingInstructions, nuint startOfRemainingOpcodes)
    {
        for (var index = 0; index < remainingInstructions.Length; index++)
        {
            switch (remainingInstructions[index])
            {
                case RelativeJumpInstruction:
                {
                    if (index + RelativeJumpInstructionLength > remainingInstructions.Length)
                    {
                        continue;
                    }

                    var relativeAddressOffset = BitConverter.ToInt32(remainingInstructions.Slice(index + 1, 4));

                    var relativeAddress = nuint.Add(startOfRemainingOpcodes, relativeAddressOffset + index);

                    var absoluteJumpAddress = GetAbsoluteJumpBytes(relativeAddress);

                    var newCustomCode = new byte
                        [remainingInstructions.Length - RelativeJumpInstructionLength + JumpAsmTemplate.Length];

                    Unsafe.WriteUnaligned(ref newCustomCode[0], remainingInstructions[..(index - 1)]);

                    Unsafe.WriteUnaligned(ref newCustomCode[index], absoluteJumpAddress);

                    for (var i = index + RelativeJumpInstructionLength; i < remainingInstructions.Length; i++)
                    {
                        newCustomCode[i] = remainingInstructions[i];
                    }

                    index -= RelativeJumpInstructionLength + JumpAsmTemplate.Length;

                    remainingInstructions = newCustomCode;

                    continue;
                }
            }
        }
    }

    private static void RemoveLastRelativeJumpInSequence(ref ReadOnlySpan<byte> customCode)
    {
        customCode = customCode[..^RelativeJumpInstructionLength].ToArray();
    }

    private static void AppendAbsoluteJumpBackAtTheEndOfSequence(ref ReadOnlySpan<byte> customCode,
        int totalAmountOfOpcodesToReplace, nuint targetAddress)
    {
        var jumpBackAddress = nuint.Add(targetAddress, totalAmountOfOpcodesToReplace);

        var jumpBackBytes = GetAbsoluteJumpBytes(jumpBackAddress);

        var customAsmInstructions = new byte[customCode.Length + jumpBackBytes.Length];

        customCode.CopyTo(customAsmInstructions.AsSpan());
        jumpBackBytes.CopyTo(customAsmInstructions.AsSpan(customCode.Length));

        customCode = customAsmInstructions;
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
}