using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="bytes"></param>
    public unsafe void WriteBytes(MemoryAddress memoryAddress, Span<byte> bytes)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var destPtr = (byte*)targetAddress;

        fixed (byte* sourcePtr = bytes)
        {
            Buffer.MemoryCopy(sourcePtr, destPtr, bytes.Length, bytes.Length);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="value"></param>
    public unsafe void WriteValue<T>(MemoryAddress memoryAddress, T value) where T : unmanaged
    {
        var targetAddress = GetTargetAddress(memoryAddress);
        
        var destPtr = (T*)targetAddress;

        *destPtr = value;
    }
}