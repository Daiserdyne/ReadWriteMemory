namespace ReadWriteMemory.Models;

/// <summary>
/// This model class stores a memory <paramref name="address"/>, the associated <paramref name="offsets"/> <c>and/or</c> <paramref name="moduleName"/>. 
/// This will be needed to calculate the base address and read/write to/from the targets process memory.
/// <example>
/// <code>MemoryAddress memoryAddress = new(0x1234567, "target_module.exe", new int[] { 0x20, 0x30, 0x40 })</code>
/// <code>MemoryAddress memoryAddress = new(0x1234567, "target_module.dll")</code>
/// <code>MemoryAddress memoryAddress = new(0x1234567, new int[] { 0x20, 0x30, 0x40 })</code>
/// <code>MemoryAddress memoryAddress = new(0x1234567)</code>
/// </example>
/// <para>See <seealso cref="MemoryAddress(long, int[])"/></para> 
/// See <seealso cref="MemoryAddress(long, string, int[])"/>
/// </summary>
public sealed class MemoryAddress
{
    /// <summary>
    /// This model class stores a memory <paramref name="address"/>, the associated <paramref name="offsets"/> and <paramref name="moduleName"/>. 
    /// This will be needed to calculate the base address and read/write to/from the targets process memory.
    /// <example>
    /// <code>MemoryAddress memoryAddress = new(0x1234567, "target_module.exe", new int[] { 0x20, 0x30, 0x40 })</code>
    /// <code>MemoryAddress memoryAddress = new(0x1234567, "target_module.dll")</code>
    /// </example>
    /// See also: <seealso cref="MemoryAddress(long, int[])"/>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="moduleName"></param>
    /// <param name="offsets"></param>
    public MemoryAddress(long address, string moduleName = "", params int[]? offsets)
    {
        Address = address;
        ModuleName = moduleName;
        Offsets = offsets;
    }

    /// <summary>
    /// This model class stores a memory <paramref name="address"/> and the associated <paramref name="offsets"/>. 
    /// This will be needed to calculate 
    /// the base address and read/write to/from the targets process memory.
    /// <example>
    /// <code>MemoryAddress memoryAddress = new(0x1234567, new int[] { 0x20, 0x30, 0x40 })</code>
    /// <code>MemoryAddress memoryAddress = new(0x1234567)</code>
    /// </example>
    /// See also: <seealso cref="MemoryAddress(long, string, int[]?)"/>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="offsets"></param>
    public MemoryAddress(long address, params int[] offsets)
    {
        Address = address;
        ModuleName = string.Empty;
        Offsets = offsets;
    }

    internal long Address { get; }
    internal string ModuleName { get; }
    internal int[]? Offsets { get; }
}