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
        
        Console.WriteLine("Versuche msg aufzurufen... Press any key to continue...");
        Console.ReadLine();
        
        var messageBoxA = new MemoryAddress("user32.dll", 0x8C4B0);
        
        _memory.CallFunction<nint, string, string, nint, int>(messageBoxA, nint.Zero, 
            "Information", "Dll injection successfull", 0x000000100);
        
        Console.WriteLine("Sollte aufgerufen worden sein.");

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(250, cancellationToken);
        }
    }
}