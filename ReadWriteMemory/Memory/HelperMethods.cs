using ReadWriteMemory.Models;
using System.Diagnostics;
using System.Numerics;
using Win32 = ReadWriteMemory.NativeImports.Win32;

namespace ReadWriteMemory;

public sealed partial class Memory
{
    private bool DeallocateMemory(nuint address)
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
    private bool IsCodeCaveAlreadyCreatedForAddress(MemoryAddress memAddress, out nuint caveAddress)
    {
        caveAddress = nuint.Zero;

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

            DeallocateMemory(caveTable.CaveAddress);
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

    private nuint GetTargetAddress(MemoryAddress memAddress)
    {
        if (!IsProcessAlive())
        {
            return nuint.Zero;
        }

        nuint baseAddress;

        var savedBaseAddress = GetBaseAddressByMemoryAddress(memAddress);

        if (savedBaseAddress != nuint.Zero)
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
                baseAddress = (nuint)(moduleAddress + address);
            }
            else
            {
                baseAddress = (nuint)memAddress.Address;
            }
        }

        var targetAddress = baseAddress;

        int[]? offsets = memAddress.Offsets;

        if (offsets is not null && offsets.Length != 0)
        {
            Win32.ReadProcessMemory(_targetProcess.Handle, targetAddress, _buffer, (nuint)_buffer.Length, IntPtr.Zero);

            targetAddress = (nuint)BitConverter.ToInt64(_buffer);

            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    targetAddress = (nuint)Convert.ToInt64((long)targetAddress + offsets[i]);
                    break;
                }

                Win32.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, offsets[i]), _buffer,
                    (nuint)_buffer.Length, IntPtr.Zero);

                targetAddress = (nuint)BitConverter.ToInt64(_buffer);
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
    private nuint GetBaseAddressByMemoryAddress(MemoryAddress memAddress)
    {
        var addressHash = CreateUniqueAddressHash(memAddress);

        foreach (var addrTable in _addressRegister)
        {
            if (addrTable.UniqueAddressHash == addressHash)
            {
                return addrTable.BaseAddress;
            }
        }

        return nuint.Zero;
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

    private nuint CalculateTargetAddress(MemoryAddress memoryAddress)
    {
        if (!IsProcessAlive())
        {
            return nuint.Zero;
        }

        var targetAddress = GetTargetAddress(memoryAddress);

        if (targetAddress == nuint.Zero)
        {
            return nuint.Zero;
        }

        return targetAddress;
    }

    private bool WriteProcessMemory(ref UIntPtr targetAddress, ref byte[] buffer)
    {
        var success = Win32.WriteProcessMemory(_targetProcess.Handle, targetAddress, buffer,
            (nuint)buffer.Length, IntPtr.Zero);

        if (!success)
        {
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
        if (_targetProcess.ProcessState.CurrentProcessState == false)
        {
            return false;
        }

        return _targetProcess.ProcessState.CurrentProcessState;
    }
}