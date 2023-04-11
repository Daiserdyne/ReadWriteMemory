using ReadWriteMemory.Models;
using ReadWriteMemory.NativeImports;
using static ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory;

internal static class CodeCave
{
    //internal static UIntPtr CreateOrResumeCodeCave(MemoryAddress memAddress, ProcessInformation processInformation, 
    //    byte[] newBytes, int replaceCount, uint size = 0x1000)
    //{
    //    if (IsCodeCaveOpen(memAddress, out var caveAddr))
    //    {
    //        _logger?.Info($"Resuming code cave for address 0x{(UIntPtr)memAddress.Address:x16}.\nCave address: 0x{caveAddr:x16}\n");
    //        return caveAddr;
    //    }

    //    var targetAddress = GetTargetAddress(memAddress);

    //    var caveAddress = UIntPtr.Zero;

    //    for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
    //    {
    //        caveAddress = VirtualAllocEx(processInformation.Handle, FindFreeBlockForRegionInMemory(targetAddress, size, processInformation), size,
    //            MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

    //        if (caveAddress == UIntPtr.Zero)
    //        {
    //            targetAddress = UIntPtr.Add(targetAddress, 0x10000);
    //        }
    //    }

    //    if (caveAddress == UIntPtr.Zero)
    //    {
    //        caveAddress = Win32.VirtualAllocEx(processInformation.Handle, UIntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    //    }

    //    int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

    //    // (to - from - 5)
    //    int offset = (int)((long)caveAddress - (long)targetAddress - 5);

    //    byte[] jmpBytes = new byte[5 + nopsNeeded];

    //    jmpBytes[0] = 0xE9;

    //    BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

    //    for (var i = 5; i < jmpBytes.Length; i++)
    //    {
    //        jmpBytes[i] = 0x90;
    //    }

    //    byte[] caveBytes = new byte[5 + newBytes.Length];
    //    offset = (int)((long)targetAddress + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

    //    newBytes.CopyTo(caveBytes, 0);
    //    caveBytes[newBytes.Length] = 0xE9;

    //    BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

    //    var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

    //    if (tableIndex != -1)
    //    {
    //        _addressRegister[tableIndex].CodeCaveTable =
    //            new(ReadBytes(targetAddress, replaceCount), caveAddress, jmpBytes);
    //    }

    //    WriteBytes(caveAddress, caveBytes);
    //    WriteBytes(targetAddress, jmpBytes);

    //    _logger?.Info($"Code cave created for address 0x{memAddress.Address:x16}.\nCustom code at cave address: " +
    //        $"0x{caveAddress:x16}.");

    //    return caveAddress;
    //}

    private static UIntPtr FindFreeBlockForRegionInMemory(UIntPtr baseAddress, uint size, ProcessInformation processInformation)
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

        while (VirtualQueryEx(processInformation.Handle, current, out MEMORY_BASIC_INFORMATION memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
            {
                return UIntPtr.Zero;
            }

            if (memoryInfos.State == Win32.MEM_FREE && memoryInfos.RegionSize > size)
            {
                UIntPtr tmpAddress;

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
}