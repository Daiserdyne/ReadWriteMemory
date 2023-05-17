using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities.CodeCave;

internal static class CodeCaveFactory
{
    internal static bool CreateCaveAndHookFunction(nuint targetAddress, nint targetProcessHandle, List<byte> newCode, int instructionOpcodesLength, 
        int totalAmountOfOpcodes, out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 4096)
    {
        caveAddress = VirtualAllocEx(targetProcessHandle, nuint.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        if (caveAddress == nuint.Zero)
        {
            jmpBytes = new byte[0];
            originalOpcodes = new byte[0];

            return false;
        }

        var startAddress = nuint.Add(targetAddress, instructionOpcodesLength);

        AppendRemainingOpcodes(targetProcessHandle, ref newCode, instructionOpcodesLength, startAddress, out var remainingOpcodes);

        newCode = CaveHelper.ConvertAllX86ToX64Calls(newCode, remainingOpcodes, instructionOpcodesLength, targetAddress);

        jmpBytes = CaveHelper.GetX64JumpBytes(caveAddress, totalAmountOfOpcodes);

        CaveHelper.ConvertX86ToX64JumpBack(ref newCode, targetAddress, instructionOpcodesLength);

        WriteProcessMemory(targetProcessHandle, caveAddress, newCode.ToArray(), newCode.Count, out _);

        WriteJumpToTargetAddress(targetProcessHandle, targetAddress, totalAmountOfOpcodes, jmpBytes, out originalOpcodes);

        return true;
    }

    private static void AppendRemainingOpcodes(nint targetProcessHandle, ref List<byte> newCode, int instructionOpcodesLength, 
        nuint startAddress, out List<byte> remainingOpcodes)
    {
        var temp = new byte[instructionOpcodesLength];

        ReadProcessMemory(targetProcessHandle, startAddress, temp, temp.Length, nint.Zero);

        newCode.AddRange(temp);

        remainingOpcodes = new List<byte>(temp);
    }

    private static void WriteJumpToTargetAddress(nint targetProcessHandle, nuint targetAddress, int opcodesToReplace, byte[] jumpBytes, 
        out byte[] originalOpcodes)
    {
        originalOpcodes = new byte[opcodesToReplace];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, opcodesToReplace, nint.Zero);

        WriteProcessMemory(targetProcessHandle, targetAddress, jumpBytes, opcodesToReplace, out _);
    }
}