using System.Numerics;
using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Shared.Entities;

namespace TestTrainer.Internal.InjectMe;

public sealed class TestTrainer
{
    private readonly RwMemory _memory = new();

    public async Task Main(CancellationToken cancellationToken)
    {
        Kernel32.AllocConsole();

        var memoryAddress = new MemoryAddress(0x5D759E0, "TOTClient-Win64-Shipping.exe",
            0x218, 0x3A0, 0x2A0, 0x1E0);

        var amogus = _memory.CallFunction<nint, string, string, nint, nint>(memoryAddress, nint.Zero, "", "", nint.Zero);

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(_memory.ReadValue<Vector3>(memoryAddress));

            await Task.Delay(500, cancellationToken);
        }
    }
}