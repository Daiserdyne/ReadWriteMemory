using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    #region Delegates

    /// <summary>
    /// A delegate that will be called after reading the unmanaged value.
    /// </summary>
    /// <param name="unmanagedValue"></param>
    public delegate void ReadValueCallback<in T>(T unmanagedValue) where T : unmanaged;

    /// <summary>
    /// A delegate that will be called after reading the byte array value.
    /// </summary>
    /// <param name="byteArrayValue"></param>
    public delegate void ReadBytesCallback(byte[] byteArrayValue);

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public unsafe T ReadValue<T>(MemoryAddress memoryAddress) where T : unmanaged
    {
        var targetAddress = GetTargetAddress(memoryAddress);
        
        return *(T*)(nuint*)targetAddress;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="memoryAddress"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public unsafe byte[] ReadBytes(MemoryAddress memoryAddress, uint length)
    {
        var targetAddress = GetTargetAddress(memoryAddress);

        var buffer = new byte[length];

        fixed (byte* bufferPtr = buffer)
        {
            Buffer.MemoryCopy((void*)targetAddress, bufferPtr, 
                length, length);
        }

        return buffer;
    }
}