using static ReadWriteMemory.NativeImports.Kernel32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory
{
    internal static bool CreateCodeCaveAndInjectCode(nuint targetAddress, nint targetProcessHandle, byte[] newCode, int replaceCount,
        out nuint caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        caveAddress = nuint.Zero;

        for (var i = 0; i < 10 && caveAddress == nuint.Zero; i++)
        {
            caveAddress = VirtualAllocEx(targetProcessHandle, FindFreeMemoryBlock(targetAddress, size, targetProcessHandle), size,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            if (caveAddress == nuint.Zero)
            {
                targetAddress = nuint.Add(targetAddress, 0x10000);
            }
        }

        if (caveAddress == nuint.Zero)
        {
            caveAddress = VirtualAllocEx(targetProcessHandle, nuint.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        }

        var nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        var offset = (int)((long)caveAddress - (long)targetAddress - 5);

        jmpBytes = new byte[5 + nopsNeeded];

        jmpBytes[0] = 0xE9;

        Buffer.BlockCopy(MemoryOperation.ConvertToByteArrayUnsafe(offset), 0, jmpBytes, 1, sizeof(int));

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        var caveBytes = new byte[5 + newCode.Length];

        offset = (int)((long)targetAddress + jmpBytes.Length - ((long)caveAddress + newCode.Length) - 5);

        Buffer.BlockCopy(newCode, 0, caveBytes, 0, newCode.Length);

        caveBytes[newCode.Length] = 0xE9;

        Buffer.BlockCopy(MemoryOperation.ConvertToByteArrayUnsafe(offset), 0, caveBytes, newCode.Length + 1, sizeof(int));

        originalOpcodes = new byte[replaceCount];

        ReadProcessMemory(targetProcessHandle, targetAddress, originalOpcodes, replaceCount, IntPtr.Zero);

        WriteProcessMemory(targetProcessHandle, caveAddress, caveBytes, (nuint)caveBytes.Length, out _);
        WriteProcessMemory(targetProcessHandle, targetAddress, jmpBytes, (nuint)jmpBytes.Length, out _); 

        return true;
    }

    private static nuint FindFreeMemoryBlock(nuint baseAddress, uint size, nint processHandle)
    {
        var minAddress = nuint.Subtract(baseAddress, 0x1000000);
        var maxAddress = nuint.Add(baseAddress, 0x1000000);

        GetSystemInfo(out SYSTEM_INFO sysInfo);

        if ((long)minAddress > (long)sysInfo.maximumApplicationAddress ||
            (long)minAddress < (long)sysInfo.minimumApplicationAddress)
        {
            minAddress = sysInfo.minimumApplicationAddress;
        }

        if ((long)maxAddress < (long)sysInfo.minimumApplicationAddress ||
            (long)maxAddress > (long)sysInfo.maximumApplicationAddress)
        {
            maxAddress = sysInfo.maximumApplicationAddress;
        }

        var current = minAddress;
        var caveAddress = nuint.Zero;

        while (VirtualQueryEx(processHandle, current, out MEMORY_BASIC_INFORMATION memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
            {
                return nuint.Zero;
            }

            if (memoryInfos.State == MEM_FREE && memoryInfos.RegionSize > size)
            {
                CalculateCaveAddress(baseAddress, size, sysInfo, ref caveAddress, memoryInfos);

                return caveAddress;
            }

            if (memoryInfos.RegionSize % sysInfo.allocationGranularity > 0)
            {
                memoryInfos.RegionSize += sysInfo.allocationGranularity - memoryInfos.RegionSize % sysInfo.allocationGranularity;
            }

            var previous = current;

            current = new nuint(memoryInfos.BaseAddress + (ulong)memoryInfos.RegionSize);

            if ((long)current >= (long)maxAddress)
            {
                return caveAddress;
            }

            if ((long)previous >= (long)current)
            {
                return caveAddress;
            }
        }

        return caveAddress;
    }

    private static nuint CalculateCaveAddress(nuint baseAddress, uint size, SYSTEM_INFO sysInfo, ref nuint caveAddress, MEMORY_BASIC_INFORMATION memoryInfos)
    {
        nuint tmpAddress;

        if ((long)memoryInfos.BaseAddress % sysInfo.allocationGranularity > 0)
        {
            tmpAddress = memoryInfos.BaseAddress;

            int offset = (int)(sysInfo.allocationGranularity - (long)tmpAddress % sysInfo.allocationGranularity);

            if (memoryInfos.RegionSize - offset >= size)
            {
                tmpAddress = nuint.Add(tmpAddress, offset);

                if ((long)tmpAddress < (long)baseAddress)
                {
                    tmpAddress = nuint.Add(tmpAddress, (int)(memoryInfos.RegionSize - offset - size));

                    if ((long)tmpAddress > (long)baseAddress)
                    {
                        tmpAddress = baseAddress;
                    }

                    tmpAddress = nuint.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                }

                if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
                {
                    caveAddress = tmpAddress;
                }
            }
        }
        else
        {
            tmpAddress = memoryInfos.BaseAddress;

            if ((long)tmpAddress < (long)baseAddress)
            {
                tmpAddress = nuint.Add(tmpAddress, (int)(memoryInfos.RegionSize - size));

                if ((long)tmpAddress > (long)baseAddress)
                {
                    tmpAddress = baseAddress;
                }

                tmpAddress = nuint.Subtract(tmpAddress, (int)(tmpAddress % sysInfo.allocationGranularity));
            }

            if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
            {
                caveAddress = tmpAddress;
            }
        }

        return tmpAddress;
    }
}