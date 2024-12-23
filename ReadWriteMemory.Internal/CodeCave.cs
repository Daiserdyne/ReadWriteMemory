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
            return table.CodeCaveTable.Value.CaveAddress;
        }

        return await Task.Run(() => CreateCodeCave(memoryAddress, customCode, amountOfOpcodesToReplace,
            totalAmountOfOpcodesToReplace, memoryToAllocate));
    }

    private nuint CreateCodeCave(MemoryAddress memoryAddress, ReadOnlySpan<byte> customCode,
        int amountOfOpcodesToReplace, int totalAmountOfOpcodesToReplace, uint memoryToAllocate = 4096)
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

        var startAddress = nuint.Add(targetAddress, amountOfOpcodesToReplace);

        var remainingOpcodesLength = totalAmountOfOpcodesToReplace - amountOfOpcodesToReplace;

        if (customCode[^RelativeJumpInstructionLength] == RelativeJumpInstruction)
        {
            customCode = RemoveLastRelativeJumpInSequence(customCode);
        }

        customCode = AppendAbsoluteJumpAtTheEndOfSequence(customCode, totalAmountOfOpcodesToReplace, targetAddress);

        var customInstructionsAtGivenAddress =
            GetAbsoluteJumpBytes(caveAddress, totalAmountOfOpcodesToReplace);


        return nuint.Zero;
    }

    private static ReadOnlySpan<byte> RemoveLastRelativeJumpInSequence(ReadOnlySpan<byte> customCode)
    {
        var sequenceWithRemovedRelativeJump = new byte[customCode[^RelativeJumpInstructionLength]];

        for (var i = 0; i < sequenceWithRemovedRelativeJump.Length; i++)
        {
            sequenceWithRemovedRelativeJump[i] = customCode[i];
        }

        return sequenceWithRemovedRelativeJump;
    }

    private static byte[] AppendAbsoluteJumpAtTheEndOfSequence(ReadOnlySpan<byte> customCode,
        int totalAmountOfOpcodesToReplace, nuint targetAddress)
    {
        var jumpBackAddress = nuint.Add(targetAddress, totalAmountOfOpcodesToReplace);

        var jumpBackBytes = GetAbsoluteJumpBytes(jumpBackAddress);

        var customAsmInstructions = new byte[customCode.Length + jumpBackBytes.Length];

        Unsafe.WriteUnaligned(ref customAsmInstructions[customCode.Length], jumpBackBytes);

        return customAsmInstructions;
    }

    private static byte[] GetAbsoluteJumpBytes(nuint jumpToAddress, int opcodesToReplace)
    {
        var jumpBytes = new byte[opcodesToReplace];

        JumpAsmTemplate.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], jumpToAddress);

        jumpBytes.AsSpan(14).Fill(0x90);

        return jumpBytes;
    }

    private static byte[] GetAbsoluteJumpBytes(nuint jumpToAddress)
    {
        var jumpBytes = new byte[JumpAsmTemplate.Length];

        JumpAsmTemplate.CopyTo(jumpBytes);

        Unsafe.WriteUnaligned(ref jumpBytes[6], jumpToAddress);

        return jumpBytes;
    }
}