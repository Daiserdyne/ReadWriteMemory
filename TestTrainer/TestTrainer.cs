using ReadWriteMemory;
using ReadWriteMemory.Models;

namespace TestTrainer;

public sealed class TestTrainer
{
    private readonly RwMemory _memory = new("signal");
    //private readonly MemoryAddress _playPosition = new(0x3, 0, 0);
    
    public async Task Main()
    {
        await Task.Run(() =>
        {
            Console.ReadLine();
            _memory.OnProcessStateChanged += OnProcessOnStateChanged;
            Console.ReadLine();
            _memory.OnProcessStateChanged += MemoryOnProcessOnStateChanged;
            Console.ReadLine();
            _memory.OnProcessStateChanged -= OnProcessOnStateChanged;
            Console.ReadLine();
            _memory.OnProcessStateChanged -= MemoryOnProcessOnStateChanged;
            Console.ReadLine();
            _memory.OnProcessStateChanged += MemoryOnProcessOnStateChanged;
            Console.ReadLine();
        });
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