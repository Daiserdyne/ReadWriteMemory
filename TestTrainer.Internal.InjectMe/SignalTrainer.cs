using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Shared.Entities;

namespace TestTrainer.Internal.InjectMe;

public sealed class SignalTrainer
{
    private readonly RwMemory _memory = new();

    public Task Main(CancellationToken _)
    {
        Kernel32.AllocConsole();
        
        var messageBoxA = new MemoryAddress("user32.dll", 0x8C4B0);

        _memory.CallFunctionCdecl<int, nuint, string, string, nint>(
            messageBoxA,
            nuint.Zero,
            "Success",
            "Dll injection was successfull", 0x000000100);
        
        return Task.CompletedTask;
    }
}