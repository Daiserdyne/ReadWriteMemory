using static ReadWriteMemory.External.NativeImports.Kernel32;

namespace ReadWriteMemory.External.Utilities.CodeCave;

internal static class CodeCaveFactory
{
    internal static bool CreateCaveAndHookFunction(nuint targetAddress, nint targetProcessHandle,
        IReadOnlyList<byte> caveCode, int instructionOpcodesLength,
        int totalAmountOfOpcodes, out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes,
        uint size = 4096)
    {
        var finalCaveCode = new List<byte>(caveCode);

        caveAddress = VirtualAllocEx(targetProcessHandle,
            nuint.Zero, size, MemCommit | MemReserve | 0x00100000,
            PageExecuteReadwrite);

        if (caveAddress == nuint.Zero)
        {
            jmpBytes = [];
            originalOpcodes = [];

            return false;
        }

        var startAddress = nuint.Add(targetAddress, instructionOpcodesLength);

        var remainingOpcodesLength = totalAmountOfOpcodes - instructionOpcodesLength;

        var insertIndex = CaveHelper.AppendJumpBack(ref finalCaveCode,
            nuint.Add(targetAddress, totalAmountOfOpcodes));

        if (insertIndex == 0)
        {
            jmpBytes = [];
            originalOpcodes = [];
            caveAddress = 0;

            return false;
        }

        ConvertAndAppendRemainingOpcodes(targetProcessHandle, ref finalCaveCode, remainingOpcodesLength, startAddress,
            insertIndex, out _);

        jmpBytes = CaveHelper.GetAbsoluteJumpBytes(caveAddress, totalAmountOfOpcodes, true);

        WriteProcessMemory(targetProcessHandle, caveAddress, finalCaveCode.ToArray(),
            (nuint)finalCaveCode.Count, out _);

        WriteJumpToTargetAddress(targetProcessHandle, targetAddress, totalAmountOfOpcodes, jmpBytes,
            out originalOpcodes);

        return true;
    }

    private static void ConvertAndAppendRemainingOpcodes(nint targetProcessHandle, ref List<byte> caveCode,
        int remainingOpcodesLength,
        nuint startAddress, int insertIndex, out List<byte> convertedRemainingOpcodes)
    {
        var remainingOpcodes = new byte[remainingOpcodesLength];

        ReadProcessMemory(targetProcessHandle, startAddress, remainingOpcodes, remainingOpcodes.Length, 
            nint.Zero);

        convertedRemainingOpcodes = CaveHelper.ConvertRemainingInstructions(remainingOpcodes, startAddress);

        caveCode.InsertRange(insertIndex, convertedRemainingOpcodes);
    }

    private static void WriteJumpToTargetAddress(nint targetProcessHandle, nuint targetAddress, int opcodesToReplace,
        byte[] jumpBytes,
        out byte[] originalOpcodes)
    {
        originalOpcodes = new byte[opcodesToReplace];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, opcodesToReplace, 
            nint.Zero);

        WriteProcessMemory(targetProcessHandle, targetAddress, jumpBytes, (nuint)opcodesToReplace, 
            out _);
    }
}