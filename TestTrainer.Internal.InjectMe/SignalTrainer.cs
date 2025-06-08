using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.Entities;

namespace TestTrainer.Internal.InjectMe;

public static class SignalTrainer
{
    private static readonly RwMemory _memory = new();

    internal static Task Main(CancellationToken _)
    {
        //Kernel32.AllocConsole();

        var messageBoxA = new MemoryAddress("user32.dll", 0x8C5D0);

        _memory.CallFunctionStdcall<int, nuint, string, string, nint>(
            messageBoxA,
            nuint.Zero,
            "Success",
            "Dll injection was successful. Amogus", 0x000000100);

        return Task.CompletedTask; 
    }
}