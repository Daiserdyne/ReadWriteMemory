using System.Collections.Frozen;
using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.Interfaces;
using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Internal.Services;
using ReadWriteMemory.Internal.Utilities;
using TestTrainer.Internal.InjectMe.Trainer;

namespace TestTrainer.Internal.InjectMe;

public class TrialsTrainer
{
    private readonly RwMemory _memory = RwMemoryHelper.RwMemory;

    private readonly FrozenDictionary<string, IMemoryTrainer> _implementedTrainer =
        new Dictionary<string, IMemoryTrainer>
        {
            {
                nameof(Freecam), new Freecam()
            }
        }.ToFrozenDictionary();

    private bool _freecamEnabled;

    internal async Task Main(CancellationToken cancellationToken)
    {
        Kernel32.AllocConsole();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await HandleTrainerTree(cancellationToken);

            await Task.Delay(1, cancellationToken);
        }
    }
    
    private async Task HandleTrainerTree(CancellationToken cancellationToken)
    {
        while (_freecamEnabled)
        {
            await HandleFreecam();
            await Task.Delay(1, cancellationToken);
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.F4))
        {
            _freecamEnabled = await _implementedTrainer[nameof(Freecam)]
                .Enable("enable_freecam");
        }
    }
    
    private async Task HandleFreecam()
    {
        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.F4))
        {
            _freecamEnabled = false;

            await _implementedTrainer[nameof(Freecam)].Disable();

            return;
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.W, false))
        {
            await _implementedTrainer[nameof(Freecam)].Enable("forward");
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.S, false))
        {
            await _implementedTrainer[nameof(Freecam)].Enable("backward");
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.E, false))
        {
            await _implementedTrainer[nameof(Freecam)].Enable("up");
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.Q, false))
        {
            await _implementedTrainer[nameof(Freecam)].Enable("down");
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.A, false))
        {
            await _implementedTrainer[nameof(Freecam)].Enable("left");
        }

        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.D, false))
        {
            await _implementedTrainer[nameof(Freecam)].Enable("right");
        }
    }
}