using System.Runtime.InteropServices;
using ReadWriteMemory.Internal.NativeImports;

namespace TestTrainer.Internal.InjectMe;

public static class EntryPointHandler
{
    private const uint DllProcessDetach = 0,
        DllProcessAttach = 1,
        DllThreadAttach = 2,
        DllThreadDetach = 3;

    [UnmanagedCallersOnly(EntryPoint = nameof(DllMain))]
    public static bool DllMain(nint module, uint reason, nint reserved)
    {
        switch (reason)
        {
            case DllProcessAttach:
            {
                Kernel32.ThreadProc threadProc = ThreadEntryPoint;
                
                Kernel32.CreateThread(
                    nint.Zero,
                    0,
                    threadProc,
                    module,
                    0,
                    out _
                );
                break;
            }
            case DllThreadAttach:
            case DllProcessDetach:
            case DllThreadDetach:
            {
                break;
            }
        }

        return true;
    }
    
    private static uint ThreadEntryPoint(nint parameter)
    {
        _ = ExecuteProgram();
        return 0;
    }
    
    private static async Task ExecuteProgram()
    {
        await new TrialsTrainer().Main(CancellationToken.None);
    }
}