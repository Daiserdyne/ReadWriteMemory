using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Main;
using ReadWriteMemory.Models;
using ReadWriteMemory.Services;

namespace ReadWriteMemory.DummyTrainer.Dl2Trainer;

internal sealed class Godmode : IMemoryTrainer
{
    private static readonly MemoryAddress _healthFunctionAddress = new(0x13A9BF1, "DeadIsland-Win64-Shipping.exe");

    private readonly byte[] _godModeBytes = new byte[] { 0x83, 0xBB, 0x50, 0x01, 0x00, 0x00, 0x01, 0x0F, 0x85, 0x0D, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xE9, 0x08, 0x00, 0x00, 0x00, 0xF3, 0x0F, 0x11, 0xB3, 0x78, 0x01, 0x00, 0x00 };

    private readonly byte[] _test = new byte[] { 0x83, 0xBB, 0x50, 0x01, 0x00, 0x00, 0x01, 0x0F, 0x85, 0x08, 0x00, 0x00, 0x00, 0xF3, 0x0F, 0x10, 0x35, 0xEB, 0x07, 0x00, 0x00, 0xF3, 0x0F, 0x11, 0xB3, 0x78, 0x01, 0x00, 0x00, };

    public ushort Id => 0;

    public string TrainerName => nameof(Godmode);

    public string Description => "You are god.";

    public bool DisableWhenDispose => true;

    private readonly RWMemory _memory;

    public Godmode()
    {
        _memory = TrainerServices.GetCreatedSingletonInstance;
    }

    public async Task Enable(params string[]? args)
    {
        await _memory.CreateOrResumeCodeCaveAsync(_healthFunctionAddress, _godModeBytes, 8, 15);
    }

    public Task Disable(params string[]? args)
    {
        _memory.CloseCodeCave(_healthFunctionAddress);

        return Task.CompletedTask;
    }
}