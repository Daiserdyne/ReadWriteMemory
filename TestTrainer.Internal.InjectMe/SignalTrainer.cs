using ReadWriteMemory.Internal;
using ReadWriteMemory.Shared.Entities;

namespace TestTrainer.Internal.InjectMe;

public sealed class SignalTrainer
{
    private readonly RwMemory _memory = new();

    public Task Main(CancellationToken cancellationToken)
    {
        var messageBoxA = new MemoryAddress("user32.dll", 0x8C4B0);
        
        _memory.CallFunction<nint, string, string, nint, int>(messageBoxA, nint.Zero, 
            "Success", "Dll injection was successfull", 0x000000100);
        
        return Task.CompletedTask;
    }
}