using ReadWriteMemory.External;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Interfaces;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;

namespace TestTrainer.External.Trainer;

public class Ghostmode : IMemoryTrainer
{
    private readonly RwMemory _memory = RwMemoryHelper.RwMemory;
    
    private static ReadOnlySpan<byte> CaveCode =>
    [
        0xE8, 0xCB, 0x3D, 0x29, 0xD7, 0xE9, 0x5E, 0xC0, 0x52, 0x00
    ];
    
    private readonly MemoryAddress _ghostFunction = new("OLGame.exe", 0x51C063);

    public int Id { get; } = 1;

    public Hotkeys.Key Hotkey => Hotkeys.Key.F8;

    public string TrainerName { get; } = nameof(Ghostmode);

    public string Description { get; } = "";
    public bool DisableWhenDispose => true;

    public async Task<bool> Enable(params string[]? args)
    {
        var cave = _memory.CreateOrResumeCodeCave(_ghostFunction, CaveCode, 5, 25);

        await Task.CompletedTask;

        return true;
    }

    public async Task<bool> Disable(params string[]? args)
    {
        _memory.CloseCodeCave(_ghostFunction);
        
        await Task.CompletedTask;

        return true;
    }
}