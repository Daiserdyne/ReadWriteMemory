using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.Entities;
using ReadWriteMemory.Internal.NativeImports;

namespace TestTrainer.Internal.InjectMe;

public static class SignalTrainer
{
    private static readonly RwMemory Memory = new();

    internal static Task Main(CancellationToken _)
    {
        Kernel32.AllocConsole();

        var messageBoxA = new MemoryAddress("user32.dll", 0x8C5D0);

        Memory.CallFunctionStdcall<int, nuint, string, string, nint>(
            messageBoxA,
            nuint.Zero,
            "Success",
            "Dll injection was successful", 0x000000100);

        return Task.CompletedTask;
    }
}