using System.Diagnostics.CodeAnalysis;
using ReadWriteMemory.Internal.Entities;

namespace ReadWriteMemory.Internal;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public partial class RwMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="bytes"></param>
    public unsafe bool WriteBytes(MemoryAddress memoryAddress, ReadOnlySpan<byte> bytes)
    {
        try
        {
            var targetAddress = GetTargetAddress(memoryAddress);

            if (targetAddress == nuint.Zero)
            {
                return false;
            }

            var destPtr = (byte*)targetAddress;

            fixed (byte* sourcePtr = bytes)
            {
                Buffer.MemoryCopy(sourcePtr, destPtr, bytes.Length, bytes.Length);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    public unsafe bool WriteValue<T>(MemoryAddress memoryAddress, T value) where T : unmanaged
    {
        try
        {
            var targetAddress = GetTargetAddress(memoryAddress);

            if (targetAddress == nuint.Zero)
            {
                return false;
            }

            var destPtr = (T*)targetAddress;

            *destPtr = value;

            return true;
        }
        catch
        {
            return false;
        }
    }
}