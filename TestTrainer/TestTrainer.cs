using System.Numerics;
using ReadWriteMemory;
using ReadWriteMemory.Models;

namespace TestTrainer;

public sealed class TestTrainer
{
    private readonly RwMemory _memory = new("Ai");
    private readonly MemoryAddress _playPosition = new(0x3, 0, 0);
    
    public TestTrainer()
    {
        _memory.ProcessOnStateChanged += OnProcessOnStateChanged;
    }

    public async Task Main()
    {
        _memory.FreezeValue<Vector3>(_playPosition, TimeSpan.FromMilliseconds(1));

        Vector3 position = new();
        
        if (_memory.ReadValueRef<Vector3>(_playPosition, ref position))
        {
            
        }
    }
    
    private void OnProcessOnStateChanged(ProgramState newprocessstate)
    {
        Console.WriteLine($"ProcessOnStateChanged: {newprocessstate}");
    }
}