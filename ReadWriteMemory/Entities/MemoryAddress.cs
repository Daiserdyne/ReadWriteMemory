namespace ReadWriteMemory.Entities;

/// <summary>
/// This record class stores address, offsets and module name. 
/// This will be needed to calculate the base address and read/write to/from the targets process memory.
/// <example>
/// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567, "elonMusk.exe", 0x42, 0x420, 0x69);</code>
/// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567, "falconheavy.dll");</code>
/// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567, 0x42, 0x420, 0x69);</code>
/// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567);</code>
/// </example>
/// <para>See <seealso cref="MemoryAddress(nuint, int[])"/></para> 
/// See <seealso cref="MemoryAddress(nuint, string, int[])"/>
/// </summary>
public readonly record struct MemoryAddress
{
    internal nuint Address { get; }

    internal string ModuleName { get; }

    internal int[]? Offsets { get; }
    
    /// <summary>
    /// This record class stores a memory <paramref name="address"/>, the associated <paramref name="offsets"/> and <paramref name="moduleName"/>. 
    /// This will be needed to calculate the base address and read/write to/from the targets process memory.
    /// <example>
    /// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567, "elonMusk.exe", 0x42, 0x420, 0x69);</code>
    /// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567, "falconheavy.dll");</code>
    /// </example>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="moduleName"></param>
    /// <param name="offsets"></param>
    public MemoryAddress(nuint address, string moduleName = "", params int[]? offsets)
    {
        Address = address;
        ModuleName = moduleName.ToLower();
        Offsets = offsets;
    }

    /// <summary>
    /// This record class stores a memory <paramref name="address"/> and the associated <paramref name="offsets"/>. 
    /// This will be needed to calculate 
    /// the base address and read/write to/from the targets process memory.
    /// <example>
    /// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567, 0x42, 0x420, 0x69);</code>
    /// <code><see cref="MemoryAddress"/> memoryAddress = new(0x1234567);</code>
    /// </example>
    /// </summary>
    /// <param name="address"></param>
    /// <param name="offsets"></param>
    public MemoryAddress(nuint address, params int[] offsets)
    {
        Address = address;
        ModuleName = string.Empty;
        Offsets = offsets;
    }
}