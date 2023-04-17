﻿using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using System.Numerics;
using System.Text;

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

            var moduleName = memAddress.ModuleName;

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
            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, _buffer);

            targetAddress = (nuint)BitConverter.ToUInt64(_buffer);

            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == offsets.Length - 1)
                {
                    targetAddress = (nuint)Convert.ToUInt64((long)targetAddress + offsets[i]);
                    break;
                }

                MemoryOperation.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, offsets[i]), _buffer);

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

    private bool CheckProcStateAndGetTargetAddress(MemoryAddress memoryAddress, out UIntPtr targetAddress)
    {
        targetAddress = UIntPtr.Zero;

        if (!IsProcessAlive())
        {
            return false;
        }

        targetAddress = CalculateTargetAddress(memoryAddress);

        if (targetAddress == UIntPtr.Zero)
        {
            return false;
        }

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

    private static void ConvertTargetValue(MemoryDataTypes type, byte[] buffer, ref object value)
    {
        value = type switch
        {
            MemoryDataTypes.Int16 => BitConverter.ToInt16(buffer, 0),
            MemoryDataTypes.Int32 => BitConverter.ToInt32(buffer, 0),
            MemoryDataTypes.Int64 => BitConverter.ToInt64(buffer, 0),
            MemoryDataTypes.Float => BitConverter.ToSingle(buffer, 0),
            MemoryDataTypes.Double => BitConverter.ToDouble(buffer, 0),
            MemoryDataTypes.String => Encoding.UTF8.GetString(buffer),
            MemoryDataTypes.ByteArray => buffer,
            _ => throw new ArgumentException("Invalid type", nameof(type))
        };
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