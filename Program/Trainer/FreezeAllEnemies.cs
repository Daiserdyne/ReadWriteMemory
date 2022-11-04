using ReadWriteMemory;
using ReadWriteMemory.Models;
using ReadWriteMemory.Trainer.Interface;

namespace Program.Trainer;

public class FreezeAllEnemies : ITrainer
{
    public string TrainerName => nameof(FreezeAllEnemies);

    public string Description => "Freezes all current enemies in your near.";

    public bool DisableWhenDispose => false;

    private readonly Memory _memory = Memory.Instance("Outlast2");
    private readonly MemoryAddress _XCoords = new(0x219FF58, "Outlast2.exe", 0x250, 0x88);

    public async Task Disable(params string[]? args)
    {
        await Task.Run(() =>
        {
            _memory.UnfreezeValue(_XCoords);
        });
    }

    public async Task Enable(params string[]? args)
    {
        await Task.Run(() =>
        {
            _memory.FreezeValue(_XCoords, 10);
        });
    }
}