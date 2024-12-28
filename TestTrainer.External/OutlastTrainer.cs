using System.Collections.Frozen;
using ReadWriteMemory.External;
using ReadWriteMemory.External.Interfaces;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;
using TestTrainer.External.NativeImports;
using TestTrainer.External.Trainer;

namespace TestTrainer.External;

public class OutlastTrainer
{
      private static Kernel32.ConsoleCtrlDelegate? _handler;

    private readonly RwMemory _memory =
        RwMemoryHelper.CreateAndGetSingletonInstance("OLGame");

    private readonly FrozenDictionary<string, IMemoryTrainer> _implementedTrainer =
        new Dictionary<string, IMemoryTrainer>
        {
            {
                nameof(Ghostmode), new Ghostmode()
            }
        }.ToFrozenDictionary();

    public async Task Main(CancellationToken cancellationToken)
    {
        _handler = Handler;
        Kernel32.SetConsoleCtrlHandler(_handler, true);

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
        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.Numpad1))
        {
            await _implementedTrainer[nameof(Ghostmode)].Enable();
        }
        if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.Numpad2))
        {
            await _implementedTrainer[nameof(Ghostmode)].Disable();
        }
    }

    public void Dispose()
    {
        _memory.Dispose();
    }
}