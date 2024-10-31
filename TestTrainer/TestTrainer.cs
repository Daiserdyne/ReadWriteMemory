using System.Collections.Frozen;
using ReadWriteMemory;
using ReadWriteMemory.Entities;
using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;
using TestTrainer.Trainer;

namespace TestTrainer;

public sealed class TestTrainer : IDisposable
{
    private readonly RwMemory _memory = RwMemoryHelper.CreateAndGetSingletonInstance("Outlast2");

    private readonly FrozenDictionary<string, IMemoryTrainer> _implementedTrainer =
        RwMemoryHelper.GetAllImplementedTrainers();

    public async Task Main(CancellationToken cancellationToken)
    {
        _memory.OnProcessStateChanged += MemoryOnProcessOnStateChanged;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F2))
            {
                await _implementedTrainer[nameof(PlayerPosition)].Enable("SavePosition");
            }

            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F3))
            {
                await _implementedTrainer[nameof(PlayerPosition)].Enable("LoadPosition");
            }

            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F4))
            {
                await _implementedTrainer[nameof(PlayerPosition)].Enable("DisplayPosition");
            }

            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F5))
            {
                await _implementedTrainer[nameof(PlayerPosition)].Enable("FreezePlayer");
            }

            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F6))
            {
                await _implementedTrainer[nameof(PlayerPosition)].Enable("DisplayPositionAsBytes");
            }

            await Task.Delay(1);
        }
    }

    private static void MemoryOnProcessOnStateChanged(ProgramState newState)
    {
        Console.WriteLine($"Process has been {newState}.");
    }

    public void Dispose()
    {
        _memory.Dispose();
    }
}