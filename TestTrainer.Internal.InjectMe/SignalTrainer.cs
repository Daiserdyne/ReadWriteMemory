using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Shared.Entities;

namespace TestTrainer.Internal.InjectMe;

public sealed class SignalTrainer
{
    private readonly RwMemory _memory = new();

    public async Task Main(CancellationToken cancellationToken)
    {
        Kernel32.AllocConsole();

        var messageBoxA = new MemoryAddress("user32.dll", 0x8C4B0);
        
        _memory.CallFunction<nint, string, string, nint, int>(messageBoxA, nint.Zero, 
            "Success", "Dll injection was successfull", 0x000000100);

        await Task.CompletedTask;
        
        Console.ReadLine();
    }
}