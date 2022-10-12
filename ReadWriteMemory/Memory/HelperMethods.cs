using ReadWriteMemory.Models;
using System.Diagnostics;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    private UIntPtr FindFreeBlockForRegion(UIntPtr baseAddress, uint size)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        var minAddress = UIntPtr.Subtract(baseAddress, 0x70000000);
        var maxAddress = UIntPtr.Add(baseAddress, 0x70000000);

        GetSystemInfo(out SYSTEM_INFO sysInfo);

        if ((long)minAddress > (long)sysInfo.maximumApplicationAddress ||
            (long)minAddress < (long)sysInfo.minimumApplicationAddress)
            minAddress = sysInfo.minimumApplicationAddress;

        if ((long)maxAddress < (long)sysInfo.minimumApplicationAddress ||
            (long)maxAddress > (long)sysInfo.maximumApplicationAddress)
            maxAddress = sysInfo.maximumApplicationAddress;

        var current = minAddress;
        var caveAddress = UIntPtr.Zero;

        while (VirtualQueryEx(_proc.Handle, current, out MEMORY_BASIC_INFORMATION memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
                return UIntPtr.Zero;  // No memory found, let windows handle

            if (memoryInfos.State == MEM_FREE && memoryInfos.RegionSize > size)
            {
                UIntPtr tmpAddress;

                if ((long)memoryInfos.BaseAddress % sysInfo.allocationGranularity > 0)
                {
                    // The whole size can not be used
                    tmpAddress = memoryInfos.BaseAddress;
                    int offset = (int)(sysInfo.allocationGranularity -
                                       (long)tmpAddress % sysInfo.allocationGranularity);

                    // Check if there is enough left
                    if (memoryInfos.RegionSize - offset >= size)
                    {
                        // yup there is enough
                        tmpAddress = UIntPtr.Add(tmpAddress, offset);

                        if ((long)tmpAddress < (long)baseAddress)
                        {
                            tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - offset - size));

                            if ((long)tmpAddress > (long)baseAddress)
                                tmpAddress = baseAddress;

                            // decrease tmpAddress until its alligned properly
                            tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                        }

                        // if the difference is closer then use that
                        if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
                            caveAddress = tmpAddress;
                    }
                }
                else
                {
                    tmpAddress = memoryInfos.BaseAddress;

                    if ((long)tmpAddress < (long)baseAddress)
                    {
                        tmpAddress = UIntPtr.Add(tmpAddress, (int)(memoryInfos.RegionSize - size));

                        if ((long)tmpAddress > (long)baseAddress)
                            tmpAddress = baseAddress;

                        tmpAddress =
                            UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                    }

                    if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)caveAddress - (long)baseAddress))
                        caveAddress = tmpAddress;
                }
            }

            if (memoryInfos.RegionSize % sysInfo.allocationGranularity > 0)
                memoryInfos.RegionSize += sysInfo.allocationGranularity - memoryInfos.RegionSize % sysInfo.allocationGranularity;

            UIntPtr previous = current;
            current = new UIntPtr((ulong)memoryInfos.BaseAddress + (ulong)memoryInfos.RegionSize);

            if ((long)current >= (long)maxAddress)
                return caveAddress;

            if ((long)previous >= (long)current)
                return caveAddress; // Overflow
        }

        return caveAddress;
    }

    private bool DeallocateMemory(UIntPtr address)
    {
        if (_proc is null)
        {
            _logger?.Error("", "_proc was null and region couldn't be dealloc."); // _proc was null and region couldn't be dealloc.
            return false;
        }

        return VirtualFreeEx(_proc.Handle, address, (UIntPtr)0, 0x8000);
    }

    /// <summary>
    /// Checks if a code cave was created in the past with the given memory address.
    /// </summary>
    /// <param name="tableIndex"></param>
    /// <param name="caveAddress"></param>
    /// <returns></returns>
    private bool IsCodeCaveOpen(MemoryAddress memAddress, out UIntPtr caveAddress)
    {
        caveAddress = UIntPtr.Zero;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
            return false;

        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
            return false;

        WriteBytes(_addressRegister[tableIndex].BaseAddress, caveTable.JmpBytes);
        caveAddress = caveTable.CaveAddress;

        return true;
    }

    /// <summary>
    /// Closes all opened code caves by patching the original bytes back and deallocating all allocated memory.
    /// </summary>
    private void CloseAllCodeCaves()
    {
        foreach (var memoryTable in _addressRegister
            .Where(addr => addr.CodeCaveTable is not null))
        {
            var baseAddress = memoryTable.BaseAddress;
            var caveTable = memoryTable.CodeCaveTable;

            if (caveTable is null)
                return;

            WriteBytes(baseAddress, caveTable.OriginalOpcodes);

            var deallocation = DeallocateMemory(caveTable.CaveAddress);

            if (!deallocation)
                _logger?.Warn("", "Couldn't free memory.");
        }
    }

    private void UnfreezeAllValues()
    {
        foreach (var freezeTokenSrc in _addressRegister
            .Where(addr => addr.FreezeTokenSrc is not null)
            .Select(addr => addr.FreezeTokenSrc))
        {
            freezeTokenSrc?.Cancel();
        }
    }

    private UIntPtr GetTargetAddress(MemoryAddress memAddress)
    {
        if (_proc is null)
            return UIntPtr.Zero;

        UIntPtr baseAddress = UIntPtr.Zero;

        var savedBaseAddress = GetBaseAddressByMemoryAddress(memAddress);

        if (savedBaseAddress != UIntPtr.Zero)
            baseAddress = savedBaseAddress;

        if (baseAddress == UIntPtr.Zero)
        {
            IntPtr moduleAddress = IntPtr.Zero;

            string moduleName = memAddress.ModuleName;

            if (moduleName != string.Empty)
                moduleAddress = GetModuleAddressByName(moduleName);

            var address = memAddress.Address;

            if (moduleAddress != IntPtr.Zero)
                baseAddress = (UIntPtr)((long)moduleAddress + address);
            else
                baseAddress = (UIntPtr)memAddress.Address;
        }

        UIntPtr targetAddress = baseAddress;
        int[]? offsets = memAddress.Offsets;

        if (offsets is not null && offsets.Length != 0)
        {
            var buffer = new byte[Size];

            ReadProcessMemory(_proc.Handle, targetAddress, buffer, (UIntPtr)Size, IntPtr.Zero);
            targetAddress = (UIntPtr)BitConverter.ToInt64(buffer);

            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    targetAddress = (UIntPtr)Convert.ToInt64((long)targetAddress + offsets[i]);
                    break;
                }

                ReadProcessMemory(_proc.Handle, UIntPtr.Add(targetAddress, offsets[i]), buffer,
                    (UIntPtr)Size, IntPtr.Zero);

                targetAddress = (UIntPtr)BitConverter.ToInt64(buffer);
            }
        }

        _addressRegister.Add(new()
        {
            MemoryAddress = memAddress,
            BaseAddress = baseAddress,
            UniqueAddressHash = CreateUniqueAddressHash(memAddress)
        });

        return targetAddress;
    }

    /// <summary>
    /// Gets the process module base address by name.
    /// </summary>
    /// <param name="moduleName">name of module</param>
    /// <returns></returns>
    private IntPtr GetModuleAddressByName(string moduleName)
    {
        if (_proc is null)
        {
            _logger?.Error("Couldn't get module address by name", $"{nameof(_proc)} was null.");
            return IntPtr.Zero;
        }

        var moduleAddress = _proc.Process.Modules.Cast<ProcessModule>()
            .FirstOrDefault(module => module.ModuleName?.ToLower() == moduleName.ToLower())?.BaseAddress;

        return moduleAddress ?? IntPtr.Zero;
    }

    /// <summary>
    /// Creates an simple and unique address hash.
    /// </summary>
    /// <param name="memAddres"></param>
    /// <returns></returns>
    private static string CreateUniqueAddressHash(MemoryAddress memAddres)
    {
        string offsetSequence = string.Empty;

        if (memAddres.Offsets is not null)
            offsetSequence = string.Concat(memAddres.Offsets);

        return memAddres.Address + memAddres.ModuleName + offsetSequence;
    }

    /// <summary>
    /// Gets the base address from the given memory address object.
    /// </summary>
    /// <param name="memAddress"></param>
    /// <returns>Base address of given memory address object.</returns>
    private UIntPtr GetBaseAddressByMemoryAddress(MemoryAddress memAddress)
    {
        string addressHash = CreateUniqueAddressHash(memAddress);

        foreach (var addrTable in _addressRegister)
        {
            if (addrTable.UniqueAddressHash == addressHash)
                return addrTable.BaseAddress;
        }

        return UIntPtr.Zero;
    }

    /// <summary>
    /// Searches the given memory address and returns the index in the address register. 
    /// </summary>
    /// <param name="memAddress"></param>
    /// <returns>The index from the given memory address in the addressRegister.</returns>
    private int GetAddressIndexByMemoryAddress(MemoryAddress memAddress)
    {
        string addressHash = CreateUniqueAddressHash(memAddress);

        for (int i = 0; i < _addressRegister.Count; i++)
        {
            if (_addressRegister[i].UniqueAddressHash == addressHash)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Reads a byte array from a given address
    /// </summary>
    /// <param name="address"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    private byte[] ReadBytes(UIntPtr address, int length)
    {
        if (_proc is null)
            return Array.Empty<byte>();

        var bytes = new byte[length];

        return ReadProcessMemory(_proc.Handle, address,
            bytes, (UIntPtr)length, IntPtr.Zero) == true ? bytes : Array.Empty<byte>();
    }

    /// <summary>
    /// Writes a byte array to a given address
    /// </summary>
    /// <param name="address">Address to write to</param>
    /// <param name="write">Byte array to write to</param>
    private void WriteBytes(UIntPtr address, byte[] write)
    {
        if (_proc is null || _proc.Process.Responding is false)
            return;

        WriteProcessMemory(_proc.Handle, address, write, (UIntPtr)write.Length, out _);
    }

    private bool IsProcessAliveOrResponding()
    {
        var procStatus = _proc is null || !_proc.Process.Responding ? false : true;
        _logger?.Error("ERROR", "Process is is closed or not responding.");

        return procStatus;
    }
}