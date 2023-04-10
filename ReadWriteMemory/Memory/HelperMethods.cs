using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using System.Diagnostics;
using System.Numerics;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    /// <summary>
    /// Creates a code cave to write custom opcodes in target process.
    /// </summary>
    /// <param name="memAddress">Address, module name and offesets</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>Created code cave address</returns>
    public Task<UIntPtr> CreateOrResumeCodeCaveAsync(MemoryAddress memAddress, byte[] newBytes,
        int replaceCount, uint size = 0x1000)
    {
        return Task.Run(() => CreateOrResumeCodeCave(memAddress, newBytes, replaceCount, size));
    }

    private UIntPtr FindFreeBlockForRegion(UIntPtr baseAddress, uint size)
    {
        if (!IsProcessAlive())
        {
            return UIntPtr.Zero;
        }

        var minAddress = UIntPtr.Subtract(baseAddress, 0x70000000);
        var maxAddress = UIntPtr.Add(baseAddress, 0x70000000);

        Win32.GetSystemInfo(out Win32.SYSTEM_INFO sysInfo);

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

        while (Win32.VirtualQueryEx(_targetProcess.Handle, current, out Win32.MEMORY_BASIC_INFORMATION memoryInfos).ToUInt64() != 0)
        {
            if ((long)memoryInfos.BaseAddress > (long)maxAddress)
            {
                return UIntPtr.Zero;  // No memory found, let windows handle
            }

            if (memoryInfos.State == Win32.MEM_FREE && memoryInfos.RegionSize > size)
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
                            {
                                tmpAddress = baseAddress;
                            }

                            // decrease tmpAddress until its alligned properly
                            tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
                        }

                        // if the difference is closer then use that
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

                        tmpAddress =
                            UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % sysInfo.allocationGranularity));
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

            UIntPtr previous = current;
            current = new UIntPtr(memoryInfos.BaseAddress + (ulong)memoryInfos.RegionSize);

            if ((long)current >= (long)maxAddress)
            {
                return caveAddress;
            }

            if ((long)previous >= (long)current)
            {
                return caveAddress; // Overflow
            }
        }

        return caveAddress;
    }

    private bool DeallocateMemory(UIntPtr address)
    {
        if (!IsProcessAlive())
        {
            return false;
        }

        return Win32.VirtualFreeEx(_targetProcess.Handle, address, 0, 0x8000);
    }

    /// <summary>
    /// Checks if a code cave was created in the past with the given memory address.
    /// </summary>
    /// <param name="memAddress"></param>
    /// <param name="caveAddress"></param>
    /// <returns></returns>
    private bool IsCodeCaveOpen(MemoryAddress memAddress, out UIntPtr caveAddress)
    {
        caveAddress = UIntPtr.Zero;

        var tableIndex = GetAddressIndexByMemoryAddress(memAddress);

        if (tableIndex == -1)
        {
            return false;
        }

        var caveTable = _addressRegister[tableIndex].CodeCaveTable;

        if (caveTable is null)
        {
            return false;
        }

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
            {
                return;
            }

            WriteBytes(baseAddress, caveTable.OriginalOpcodes);

            var deallocation = DeallocateMemory(caveTable.CaveAddress);

            if (!deallocation)
            {
                _logger?.Warn("Couldn't free memory.");
            }
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
        if (!IsProcessAlive())
        {
            return UIntPtr.Zero;
        }

        UIntPtr baseAddress;

        var savedBaseAddress = GetBaseAddressByMemoryAddress(memAddress);

        if (savedBaseAddress != UIntPtr.Zero)
        {
            baseAddress = savedBaseAddress;
        }
        else
        {
            var moduleAddress = IntPtr.Zero;

            string moduleName = memAddress.ModuleName;

            if (moduleName != string.Empty)
            {
                moduleAddress = GetModuleAddressByName(moduleName);
            }

            var address = memAddress.Address;

            if (moduleAddress != IntPtr.Zero)
            {
                baseAddress = (UIntPtr)(moduleAddress + address);
            }
            else
            {
                baseAddress = (UIntPtr)memAddress.Address;
            }
        }

        var targetAddress = baseAddress;

        int[]? offsets = memAddress.Offsets;

        if (offsets is not null && offsets.Length != 0)
        {
            Win32.ReadProcessMemory(_targetProcess.Handle, targetAddress, _buffer, (UIntPtr)_buffer.Length, IntPtr.Zero);

            targetAddress = (UIntPtr)BitConverter.ToInt64(_buffer);

            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    targetAddress = (UIntPtr)Convert.ToInt64((long)targetAddress + offsets[i]);
                    break;
                }

                Win32.ReadProcessMemory(_targetProcess.Handle, UIntPtr.Add(targetAddress, offsets[i]), _buffer,
                    (UIntPtr)_buffer.Length, IntPtr.Zero);

                targetAddress = (UIntPtr)BitConverter.ToInt64(_buffer);
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
        if (!IsProcessAlive())
        {
            return IntPtr.Zero;
        }

        var moduleAddress = _targetProcess.Process.Modules.Cast<ProcessModule>()
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
        var offsetSequence = string.Empty;

        if (memAddres.Offsets is not null)
        {
            offsetSequence = string.Concat(memAddres.Offsets);
        }

        return memAddres.Address + memAddres.ModuleName + offsetSequence;
    }

    /// <summary>
    /// Gets the base address from the given memory address object.
    /// </summary>
    /// <param name="memAddress"></param>
    /// <returns>Base address of given memory address object.</returns>
    private UIntPtr GetBaseAddressByMemoryAddress(MemoryAddress memAddress)
    {
        var addressHash = CreateUniqueAddressHash(memAddress);

        foreach (var addrTable in _addressRegister)
        {
            if (addrTable.UniqueAddressHash == addressHash)
            {
                return addrTable.BaseAddress;
            }
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
        var addressHash = CreateUniqueAddressHash(memAddress);

        for (int i = 0; i < _addressRegister.Count; i++)
        {
            if (_addressRegister[i].UniqueAddressHash == addressHash)
            {
                return i;
            }
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
        if (!IsProcessAlive())
        {
            return Array.Empty<byte>();
        }

        var bytes = new byte[length];

        return Win32.ReadProcessMemory(_targetProcess.Handle, address,
            bytes, (UIntPtr)length, IntPtr.Zero) == true ? bytes : Array.Empty<byte>();
    }

    private UIntPtr CalculateTargetAddress(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive())
        {
            return UIntPtr.Zero;
        }

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
        {
            return UIntPtr.Zero;
        }

        return targetAddress;
    }

    private bool WriteProcessMemory(ref UIntPtr targetAddress, ref byte[] buffer)
    {
        var success = Win32.WriteProcessMemory(_targetProcess.Handle, targetAddress, buffer,
            (UIntPtr)buffer.Length, IntPtr.Zero);

        if (!success)
        {
            _logger?.Error("Writing to process memory failed. ReadProcessMemory returned false.");
            return false;
        }

        return true;
    }

    private static Vector3 CalculateNewPosition(Quaternion rotation, Vector3 currentPosition, float distance)
    {
        var forward = Vector3.UnitZ;

        var direction = Vector3.Transform(forward, rotation);

        var newPosition = currentPosition + (direction * distance);

        return newPosition;
    }

    private bool IsProcessAlive()
    {
        if (_procState.CurrentProcessState == false)
        {
            _logger?.Warn($"Target process \"{_targetProcess.ProcessName}\" isn't running.");
            return false;
        }

        return _procState.CurrentProcessState;
    }
}