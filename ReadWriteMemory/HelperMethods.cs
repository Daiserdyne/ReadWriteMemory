using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using System.Numerics;

namespace ReadWriteMemory;

public sealed partial class Memory
{
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
            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, _buffer, (UIntPtr)_buffer.Length);

            targetAddress = (nuint)BitConverter.ToInt64(_buffer);

            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    targetAddress = (nuint)Convert.ToInt64((long)targetAddress + offsets[i]);
                    break;
                }

                MemoryOperation.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, offsets[i]), _buffer, (nuint)_buffer.Length);

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

        return _targetProcess.Modules?[moduleName.ToLower()].BaseAddress ?? IntPtr.Zero;
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

    private static Vector3 CalculateNewPosition(Quaternion rotation, Vector3 currentPosition, float distance)
    {
        var forward = Vector3.UnitZ;

        var direction = Vector3.Transform(forward, rotation);

        var newPosition = currentPosition + (direction * distance);

        return newPosition;
    }
}