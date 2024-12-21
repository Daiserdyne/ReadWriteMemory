using System.Runtime.InteropServices;

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
                Task.Run(ExecuteProgram);
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

    private static async Task ExecuteProgram()
    {
        await TrialsTrainer.Main(CancellationToken.None);
    }
}