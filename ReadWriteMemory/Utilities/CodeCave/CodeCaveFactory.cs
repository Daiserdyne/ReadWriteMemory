using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class CodeCaveFactory
{
    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int instructionOpcodesLength, int totalAmountOfOpcodes,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        jmpBytes = new byte[0];
        originalOpcodes = new byte[0];

        caveAddress = VirtualAllocEx(targetProcessHandle, nuint.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        if (caveAddress == nuint.Zero)
        {
            return false;
        }

        var startAddress = nuint.Add(targetAddress, instructionOpcodesLength);

        var remainingOpcodes = new byte[totalAmountOfOpcodes - instructionOpcodesLength];

        ReadProcessMemory(targetProcessHandle, startAddress, remainingOpcodes, instructionOpcodesLength, nint.Zero);

        var tempNewCode = new byte[newCode.Length + remainingOpcodes.Length];
        Buffer.BlockCopy(newCode, 0, tempNewCode, 0, newCode.Length);

        newCode = tempNewCode;

        for (int i = 0; i < remainingOpcodes.Length; i++)
        {
            newCode[newCode.Length - 1 - i] = remainingOpcodes[remainingOpcodes.Length - 1 - i];
        }

        newCode = CaveHelper.ConvertAllX86ToX64Calls(newCode, remainingOpcodes, instructionOpcodesLength, targetAddress);

        jmpBytes = CaveHelper.GetX64JumpBytes(caveAddress, totalAmountOfOpcodes);

        var jumpBack = CaveHelper.GetX64JumpBytes(nuint.Add(targetAddress, totalAmountOfOpcodes), totalAmountOfOpcodes, isJumpBack: true);

        tempNewCode = new byte[newCode.Length + jumpBack.Length];
        Buffer.BlockCopy(newCode, 0, tempNewCode, 0, newCode.Length);

        newCode = tempNewCode;

        for (int i = 0; i < jumpBack.Length; i++)
        {
            newCode[newCode.Length - 1 - i] = jumpBack[jumpBack.Length - 1 - i];
        }

        WriteProcessMemory(targetProcessHandle, caveAddress, newCode, (nuint)newCode.Length, out _);

        WriteJumpToAddress(targetProcessHandle, targetAddress, (nuint)totalAmountOfOpcodes, jmpBytes, out originalOpcodes);

        return true;
    }

    private static void WriteJumpToAddress(nint targetProcessHandle, nuint targetAddress, nuint replaceCount, byte[] jumpBytes, out byte[] originalOpcodes)
    {
        originalOpcodes = new byte[replaceCount];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, (int)replaceCount, nint.Zero);

        WriteProcessMemory(targetProcessHandle, targetAddress, jumpBytes, replaceCount, out _);
    }
}