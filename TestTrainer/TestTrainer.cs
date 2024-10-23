using ReadWriteMemory;

namespace TestTrainer;

public sealed class TestTrainer
{
    private readonly RwMemory _memory = new("Ai");

    public TestTrainer()
    {
        _memory.ProcessOnStateChanged += OnProcessOnStateChanged;
    }

    private void OnProcessOnStateChanged(bool newprocessstate)
    {
        Console.WriteLine($"ProcessOnStateChanged: {newprocessstate}");
    }
}