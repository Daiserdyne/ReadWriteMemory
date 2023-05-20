﻿using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class CodeCaveFactory
{
    internal static bool CreateCaveAndHookFunction(nuint targetAddress, nint targetProcessHandle, IReadOnlyList<byte> caveCode, int instructionOpcodesLength, 
        int totalAmountOfOpcodes, out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 4096)
    {
        var finalCaveCode = new List<byte>(caveCode);

        caveAddress = VirtualAllocEx(targetProcessHandle, nuint.Zero, size, MEM_COMMIT | MEM_RESERVE | 0x00100000, PAGE_EXECUTE_READWRITE);

        if (caveAddress == nuint.Zero)
        {
            jmpBytes = new byte[0];
            originalOpcodes = new byte[0];

            return false;
        }

        var startAddress = nuint.Add(targetAddress, instructionOpcodesLength);

        var remainingOpcodesLength = totalAmountOfOpcodes - instructionOpcodesLength;

        var insertIndex = CaveHelper.AppendJumpBack(ref finalCaveCode, nuint.Add(targetAddress, totalAmountOfOpcodes));

        if (insertIndex == default)
        {
            jmpBytes = new byte[0];
            originalOpcodes = new byte[0];
            caveAddress = default;

            return false;
        }

        ConvertAndAppendRemainingOpcodes(targetProcessHandle, ref finalCaveCode, remainingOpcodesLength, startAddress, insertIndex, out _);

        jmpBytes = CaveHelper.GetAbsoluteJumpBytes(caveAddress, totalAmountOfOpcodes, true);

        WriteProcessMemory(targetProcessHandle, caveAddress, finalCaveCode.ToArray(), finalCaveCode.Count, out _);

        WriteJumpToTargetAddress(targetProcessHandle, targetAddress, totalAmountOfOpcodes, jmpBytes, out originalOpcodes);

        return true;
    }

    private static void ConvertAndAppendRemainingOpcodes(nint targetProcessHandle, ref List<byte> caveCode, int remainingOpcodesLength, 
        nuint startAddress, int insertIndex, out List<byte> convertedRemainingOpcodes)
    {
        var remainingOpcodes = new byte[remainingOpcodesLength];

        ReadProcessMemory(targetProcessHandle, startAddress, remainingOpcodes, remainingOpcodes.Length, nint.Zero);

        convertedRemainingOpcodes = CaveHelper.ConvertRemainingInstructions(remainingOpcodes, startAddress);

        caveCode.InsertRange(insertIndex, convertedRemainingOpcodes);
    }

    private static void WriteJumpToTargetAddress(nint targetProcessHandle, nuint targetAddress, int opcodesToReplace, byte[] jumpBytes, 
        out byte[] originalOpcodes)
    {
        originalOpcodes = new byte[opcodesToReplace];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, opcodesToReplace, nint.Zero);

        WriteProcessMemory(targetProcessHandle, targetAddress, jumpBytes, opcodesToReplace, out _);
    }
}