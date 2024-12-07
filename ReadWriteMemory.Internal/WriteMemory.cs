using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="bytes"></param>
    public unsafe bool WriteBytes(MemoryAddress memoryAddress, Span<byte> bytes)
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
        }
        catch
        {
            return false;
        }

        return true;
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
        }
        catch
        {
            return false;
        }

        return true;
    }
}