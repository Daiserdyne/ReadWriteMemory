using System.Numerics;
using ReadWriteMemory;
using ReadWriteMemory.Models;

namespace TestTrainer;

public sealed class TestTrainer
{
    private readonly RwMemory _memory = new("signal");
    private readonly MemoryAddress _playPosition = new(0x3, 0, 0);
    
    public TestTrainer()
    {
    }

    public async Task Main()
    {
        Console.ReadLine();
        _memory.ProcessOnStateChanged += OnProcessOnStateChanged;
        Console.ReadLine();
        _memory.ProcessOnStateChanged += MemoryOnProcessOnStateChanged;
        Console.ReadLine();
        _memory.ProcessOnStateChanged -= OnProcessOnStateChanged;
        Console.ReadLine();
        _memory.ProcessOnStateChanged -= MemoryOnProcessOnStateChanged;
        Console.ReadLine();
        _memory.ProcessOnStateChanged += MemoryOnProcessOnStateChanged;
        Console.ReadLine();
    }

    private void MemoryOnProcessOnStateChanged(ProgramState newprocessstate)
    {
        Console.WriteLine($"ProcessOnStateChanged: {newprocessstate}");
    }

    private void OnProcessOnStateChanged(ProgramState newprocessstate)
    {
        Console.WriteLine($"ProcessOnStateChanged: {newprocessstate}");
    }
}