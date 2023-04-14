﻿using ReadWriteMemory.Models;
using static ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory.Utilities;

internal static class CodeCaveFactory
{
    internal static bool CreateCodeCaveAndInjectCode(UIntPtr targetAddress, IntPtr targetProcessHandle, byte[] newCode, int replaceCount,
        out UIntPtr caveAddress, out byte[] originalOpcodes, out byte[] jmpBytes, uint size = 0x1000)
    {
        caveAddress = UIntPtr.Zero;
        originalOpcodes = new byte[0];

        for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
        {
            caveAddress = VirtualAllocEx(targetProcessHandle, FindFreeMemoryBlock(targetAddress, size, targetProcessHandle), size,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            if (caveAddress == UIntPtr.Zero)
            {
                targetAddress = UIntPtr.Add(targetAddress, 0x10000);
            }
        }

        if (caveAddress == UIntPtr.Zero)
        {
            caveAddress = VirtualAllocEx(targetProcessHandle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        }

        int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        // (to - from - 5)
        int offset = (int)((long)caveAddress - (long)targetAddress - 5);

        jmpBytes = new byte[5 + nopsNeeded];

        jmpBytes[0] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[5 + newCode.Length];
        offset = (int)((long)targetAddress + jmpBytes.Length - ((long)caveAddress + newCode.Length) - 5);

        newCode.CopyTo(caveBytes, 0);
        caveBytes[newCode.Length] = 0xE9;

        BitConverter.GetBytes(offset).CopyTo(caveBytes, newCode.Length + 1);

        var readBytes = new byte[replaceCount];

        _ = ReadProcessMemory(targetProcessHandle, targetAddress, readBytes, (UIntPtr)replaceCount, IntPtr.Zero)
            == true ? readBytes : Array.Empty<byte>();

        var caveTable = new CodeCaveTable(readBytes, caveAddress, jmpBytes);

        WriteProcessMemory(targetProcessHandle, caveAddress, caveBytes, (UIntPtr)caveBytes.Length, out _);
        WriteProcessMemory(targetProcessHandle, targetAddress, jmpBytes, (UIntPtr)jmpBytes.Length, out _);

        //_logger?.Info($"Code cave created for address 0x{memAddress.Address:x16}.\nCustom code at cave address: " +
        //    $"0x{caveAddress:x16}.");

        return true;
    }

    private static UIntPtr FindFreeMemoryBlock(UIntPtr baseAddress, uint size, IntPtr processHandle)
    {
        var minAddress = UIntPtr.Subtract(baseAddress, 0x70000000);
        var maxAddress = UIntPtr.Add(baseAddress, 0x70000000);

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
        var caveAddress = UIntPtr.Zero;

        while (VirtualQueryEx(processHandle, current, out MEMORY_BASIC_INFORMATION memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
            {
                return UIntPtr.Zero;
            }

            if (memoryInfos.State == MEM_FREE && memoryInfos.RegionSize > size)
            {
                CalculateCaveAddress(baseAddress, size, sysInfo, ref caveAddress, memoryInfos);
            }

            if (memoryInfos.RegionSize % sysInfo.allocationGranularity > 0)
            {
                memoryInfos.RegionSize += sysInfo.allocationGranularity - memoryInfos.RegionSize % sysInfo.allocationGranularity;
            }

            var previous = current;

            current = new UIntPtr(memoryInfos.BaseAddress + (ulong)memoryInfos.RegionSize);

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
                tmpAddress = UIntPtr.Add(tmpAddress, offset);

                if ((long)tmpAddress < (long)baseAddress)
                {
                    tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - offset - size));

                    if ((long)tmpAddress > (long)baseAddress)
                    {
                        tmpAddress = baseAddress;
                    }

                    tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
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
                tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - size));

                if ((long)tmpAddress > (long)baseAddress)
                {
                    tmpAddress = baseAddress;
                }

                tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
            }

            if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
            {
                caveAddress = tmpAddress;
            }
        }

        return tmpAddress;
    }
}