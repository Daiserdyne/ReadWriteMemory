using System.Collections.Frozen;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Interfaces;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;
using TestTrainer.External.NativeImports;
using TestTrainer.External.Trainer;
using RwMemory = ReadWriteMemory.External.RwMemory;

namespace TestTrainer.External;

public sealed class TrialsTrainer : IDisposable
{
    private static Kernel32.ConsoleCtrlDelegate? _handler;

    private readonly RwMemory _memory =
        RwMemoryHelper.CreateAndGetSingletonInstance("TOTClient-Win64-Shipping");

    private readonly FrozenDictionary<string, IMemoryTrainer> _implementedTrainer =
        new Dictionary<string, IMemoryTrainer>
        {
            {
                nameof(Freecam), new Freecam()
            }
        }.ToFrozenDictionary();

    private bool _freecamEnabled;

    public async Task Main(CancellationToken cancellationToken)
    {
        _handler = Handler;
        Kernel32.SetConsoleCtrlHandler(_handler, true);

        _memory.OnProcessStateChanged += OnProcessStateChanged;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_memory.IsProcessAlive)
            {
                await HandleTrainerTree(cancellationToken);
            }

            await Task.Delay(1, cancellationToken);
        }
    }

    private bool Handler(Kernel32.CtrlTypes ctrlType)
    {
        if (ctrlType is Kernel32.CtrlTypes.CTRL_CLOSE_EVENT
            or Kernel32.CtrlTypes.CTRL_C_EVENT)
        {
            _memory.Dispose();
        }

        return false;
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
    
    private void OnProcessStateChanged(ProgramState state)
    {
        if (state == ProgramState.Closed)
        {
            _freecamEnabled = false;
        }
    }

    public void Dispose()
    {
        _memory.Dispose();
    }
}