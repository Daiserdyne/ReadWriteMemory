using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ReadWriteMemory.Internal.NativeImports;

namespace TestTrainer.Internal.InjectMe;

public static class EntryPoint
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(DllMain))]
    public static bool DllMain(nint module, uint reason, nint reserved)
    {
        if (reason == 1)
        {
            _ = Task.Run(async () =>
            {
                await new SignalTrainer().Main(default);
            });
        }
        
        return true; 
    }
}