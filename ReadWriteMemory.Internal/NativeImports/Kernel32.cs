﻿using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Global

#pragma warning disable CS1591

namespace ReadWriteMemory.Internal.NativeImports;

public static partial class Kernel32
{
    internal const uint PageNoAccess = 0x01;
    internal const uint MemCommit = 0x1000;
    internal const uint MemReserve = 0x2000;
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial int VirtualQuery(nuint lpAddress, out MemoryBasicInformation lpBuffer, uint dwLength);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllocConsole();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint VirtualAlloc(
        nint lpAddress,
        uint dwSize,
        AllocationType flAllocationType,
        MemoryProtection flProtect);
    
    [Flags]
    internal enum AllocationType : uint
    {
        Commit = 0x00001000,
        Reserve = 0x00002000,
    }

    [Flags]
    internal enum MemoryProtection : uint
    {
        ExecuteReadWrite = 0x40
    }
}