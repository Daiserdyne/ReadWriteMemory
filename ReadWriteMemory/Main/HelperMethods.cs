using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using System.Numerics;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    private unsafe nuint GetTargetAddress(MemoryAddress memAddress)
    {
        nuint baseAddress;

        var savedBaseAddress = GetBaseAddressByMemoryAddress(memAddress);

        if (savedBaseAddress != nuint.Zero)
        {
            baseAddress = savedBaseAddress;
        }
        else
        {
            var moduleAddress = IntPtr.Zero;

            var moduleName = memAddress.ModuleName;

            if (!string.IsNullOrEmpty(moduleName))
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

        var buffer = new byte[8];

        if (offsets is not null && offsets.Any())
        {
            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer);

            MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);

            for (short i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    targetAddress = (nuint)Convert.ToUInt64((long)targetAddress + offsets[i]);

                    break;
                }

                MemoryOperation.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, offsets[i]), buffer);

                MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);
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

    private bool GetTargetAddress(MemoryAddress memoryAddress, out nuint targetAddress)
    {
        if (!IsProcessAlive())
        {
            targetAddress = default;

            return false;
        }

        targetAddress = GetTargetAddress(memoryAddress);

        return true;
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

        return _targetProcess.Modules?[moduleName].BaseAddress ?? IntPtr.Zero;
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

        for (ushort i = 0; i < _addressRegister.Count; i++)
        {
            if (_addressRegister[i].UniqueAddressHash == addressHash)
            {
                return _addressRegister[i].BaseAddress;
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

        for (ushort i = 0; i < _addressRegister.Count; i++)
        {
            if (_addressRegister[i].UniqueAddressHash == addressHash)
            {
                return i;
            }
        }

        return -1;
    }

    private static Vector3 CalculateNewPosition(Quaternion rotation, Vector3 currentPosition, float distance)
    {
        var forward = Vector3.UnitZ;

        var direction = Vector3.Transform(forward, rotation);

        var newPosition = currentPosition + (direction * distance);

        return newPosition;
    }
}