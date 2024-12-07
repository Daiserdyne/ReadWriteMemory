using System.Numerics;
using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.Entities;
using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Shared.Entities;

namespace TestTrainer.Internal.InjectMe;

public sealed class TestTrainer
{
    private readonly MemoryAddress _cameraCoordinatesAddress =
        new("TOTClient-Win64-Shipping.exe",
            0x05D759E0, 0x218, 0x3A0, 0x2A0, 0x1E0);
    
    private readonly RwMemory _memory = new();

    public async Task Main(CancellationToken cancellationToken)
    {
        Kernel32.AllocConsole();

        var messageBoxA = new MemoryAddress("user32.dll", 0x8C4B0);

        _memory.CallFunction<nint, string, string, nint, int>(messageBoxA, 
            nint.Zero, 
            "Information", 
            "Dll injection successfull", 
            0x000000100, CallConv.Stdcall);

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(_memory.ReadValue<Vector3>(_cameraCoordinatesAddress));
            
            await Task.Delay(250, cancellationToken);
        }
    }
}