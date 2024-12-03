using System.Runtime.InteropServices;
using ReadWriteMemory.Shared.Entities;

namespace ReadWriteMemory.Internal;

public partial class RwMemory
{
    public unsafe Out CallFunction<In, In2, Out>(MemoryAddress memoryAddress, CallingConvention convention,
        In arg1, In2 arg2, Out arg3)
    {
        var targetAddress = GetTargetAddress(memoryAddress);
        
        var func = (delegate* unmanaged[Cdecl]<In, In2, Out>)targetAddress;

        return func(arg1, arg2);
    }
}