using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestTrainer.Internal.InjectMe;

public class EntryPoint
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvFastcall)], EntryPoint = nameof(DllMain))]
    public static int DllMain(nint hModule, uint ulReasonForCall, nint lpReserved)
    {
        if (ulReasonForCall == 1)
        {
            _ = Task.Run(() => new TestTrainer().Main(default));
        }

        return 1; 
    }
}