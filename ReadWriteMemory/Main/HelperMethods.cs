using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.Main;

public sealed partial class RWMemory
{
    private unsafe nuint GetTargetAddress(MemoryAddress memoryAddress)
    {
        nuint baseAddress = default;

        if (!_memoryRegister.ContainsKey(memoryAddress))
        {
            return baseAddress;
        }

        var savedBaseAddress = _memoryRegister[memoryAddress].BaseAddress;

        if (savedBaseAddress != nuint.Zero)
        {
            baseAddress = savedBaseAddress;
        }
        else
        {
            var moduleAddress = IntPtr.Zero;

            var moduleName = memoryAddress.ModuleName;

            if (!string.IsNullOrEmpty(moduleName) && _targetProcess.Modules.ContainsKey(moduleName))
            {
                moduleAddress = _targetProcess.Modules[moduleName];
            }

            var address = memoryAddress.Address;

            if (moduleAddress != IntPtr.Zero)
            {
                baseAddress = (nuint)(moduleAddress + address);
            }
            else
            {
                baseAddress = (nuint)memoryAddress.Address;
            }
        }

        var targetAddress = baseAddress;

        if (memoryAddress.Offsets is not null && memoryAddress.Offsets.Any())
        {
            var buffer = new byte[nint.Size];

            MemoryOperation.ReadProcessMemory(_targetProcess.Handle, targetAddress, buffer);

            MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);

            for (ushort index = 0; index < memoryAddress.Offsets.Length; index++)
            {
                if (index == memoryAddress.Offsets.Length - 1)
                {
                    targetAddress = (nuint)Convert.ToUInt64((long)targetAddress + memoryAddress.Offsets[index]);

                    break;
                }

                MemoryOperation.ReadProcessMemory(_targetProcess.Handle, nuint.Add(targetAddress, memoryAddress.Offsets[index]), buffer);

                MemoryOperation.ConvertBufferUnsafe(buffer, out targetAddress);
            }
        }

        _memoryRegister.Add(memoryAddress, new()
        {
            MemoryAddress = memoryAddress,
            BaseAddress = baseAddress
        });

        return targetAddress;
    }

    private bool GetTargetAddress(MemoryAddress memoryAddress, out nuint targetAddress)
    {
        if (!IsProcessAlive)
        {
            targetAddress = default;

            return false;
        }

        targetAddress = GetTargetAddress(memoryAddress);

        return true;
    }

    private MemoryAddressTable? GetMemoryTableByMemoryAddress(MemoryAddress memoryAddress)
    {
        if (_memoryRegister.TryGetValue(memoryAddress, out var memoryAddressTable))
        {
            return memoryAddressTable;
        }

        return null;
    }
}