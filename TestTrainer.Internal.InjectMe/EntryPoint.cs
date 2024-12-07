using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestTrainer.Internal.InjectMe;

public static class EntryPoint
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)], EntryPoint = nameof(DllMain))]
    public static int DllMain(nint hModule, uint ulReasonForCall, nint lpReserved)
    {
        if (ulReasonForCall == 1)
        {
            _ = Task.Run(() => new SignalTrainer().Main(default));
        }

        return 1; 
    }
}