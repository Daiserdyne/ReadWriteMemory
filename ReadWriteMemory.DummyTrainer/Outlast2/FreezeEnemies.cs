using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Main;
using ReadWriteMemory.Models;
using ReadWriteMemory.Services;

namespace ReadWriteMemory.DummyTrainer.Outlast2;

internal sealed class FreezeEnemies : IMemoryTrainer
{
    private readonly RWMemory _memory;

    private readonly List<byte> freezebytesX = new() { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x84, 0x0A, 0x00, 0x00, 0x00, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xE9, 0x09, 0x00, 0x00, 0x00, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xE9, 0x40, 0xC5, 0x57, 0x00 };

    private readonly MemoryAddress _walkFunction = new(0x56C55F, "Outlast2.exe");

    public ushort Id => 0;

    public string TrainerName => nameof(FreezeEnemies);

    public string Description => "Freezes the enemies so they can'r walk/run.";

    public bool DisableWhenDispose => true;

    public FreezeEnemies()
    {
        _memory = TrainerServices.GetCreatedSingletonInstance;
    }

    public async Task Enable(params string[]? args)
    {
        await _memory.CreateOrResumeCodeCaveAsync(_walkFunction, freezebytesX, 9, 14);
    }

    public Task Disable(params string[]? args)
    {
        _memory.CloseCodeCave(_walkFunction);

        return Task.CompletedTask;
    }
}