# ReadWriteMemory.External

**ReadWriteMemory.External** is a high-performance C# library for external memory manipulation in Windows processes (Perhaps for Linux also in the future).  
It is designed to be as fast and efficient as possible, minimizing overhead so that the main bottleneck is the Windows API itself.

**Features:**
- **ReadMemory / WriteMemory** for any `unmanaged` type (not limited to primitive types — supports custom structs, vectors, etc.).
- **Freeze Memory** – lock values in place, optionally with conditions until specific events occur.
- **Constant Read** – continuously read values at configurable intervals with multiple reading modes.
- **Code Injection / CodeCave** – insert and execute custom ASM code inside the target process.
- **Advanced Memory Control** – beyond simple read/write, supports extended operations for flexible trainer or tool creation.

This library is ideal for building trainers, debugging tools, or experimental projects that require precise and fast access to process memory (e.g., in single-player games or test environments).

> ⚠️ **Legal & ethical notice:**  
> Use this library **only** for legitimate, offline/single‑player scenarios (debugging, modding, personal tooling). Misuse in online/multiplayer environments may violate ToS or laws.

---

# 📦 Installation

Clone or add as a submodule:

```bash
dotnet add package ReadWriteMemory.External 
```

---

# 📄 Full Documentation

 

---

# Simple example of program.cs / Entry class
```csharp
using ReadWriteMemory.External;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Interfaces;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;

namespace SuperTrainer;

public sealed class Program : IAsyncDisposable
{
    private readonly RwMemory _memory =
        RwMemoryHelper.CreateAndGetSingletonInstance("Target-Program");

    // This does not work with Aot. In case you want to use Aot, you need to add
    // every single trainer by yourself in the list since reflection does not work with Aot.
    private readonly FrozenDictionary<string, IMemoryTrainer> _implementedTrainer =
        RwMemoryHelper.GetAllImplementedTrainers();

    private bool _freecamEnabled;

    public async Task Main(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_memory.IsProcessAlive)
            {
                await HandleTrainerTree(cancellationToken);
            }

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

    public async ValueTask DisposeAsync()
    {
        await _memory.DisposeAsync();
    }
}
```

---

# Simple Example of a Trainer class
### Implements IMemoryTrainer

```csharp
using System.Numerics;
using ReadWriteMemory.External;
using ReadWriteMemory.External.Entities;
using ReadWriteMemory.External.Interfaces;
using ReadWriteMemory.External.Services;
using ReadWriteMemory.External.Utilities;

public sealed class Freecam : IMemoryTrainer
{
    private readonly RwMemory _memory = RwMemoryHelper.RwMemory;

    private readonly MemoryAddress _cameraFunctionAddress =
        new("Target.exe", 0x19CC6F9);

    private readonly MemoryAddress _cameraCoordinatesAddress =
        new("Target.exe", 0x680D050,
            0x220, 0x3B0, 0x2A0, 0x1E0);

    // Left and right view
    private readonly MemoryAddress _cameraPitchAddress =
        new("Target.exe", 0x680D050,
            0x210, 0x260, 0x2A0, 0x6C0, 0x68, 0x4F8, 0x74);
    
    // Up and down view
    private readonly MemoryAddress _cameraYawAddress =
        new("Target.exe", 0x680D050,
            0x210, 0x8F8, 0x20, 0x29C);

    private static ReadOnlySpan<byte> CustomCameraFunctionShellCode =>
    [
        0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
    ];

    private Vector3 _currentCameraPosition = Vector3.Zero;
    private float _currentPitch;
    private float _currentYaw;

    public Freecam() => _memory.OnReInitializeTargetProcess += OnReInitializeTargetProcess;

    public int Id => 4;

    public Hotkeys.Key Hotkey => Hotkeys.Key.F4;

    public string TrainerName => nameof(Freecam);

    public string Description => "Ingame freecam.";

    public bool DisableWhenDispose => true;

    private void RefreshYaw(float newYaw) => _currentYaw = newYaw;

    private void RefreshPitch(float newPitch) => _currentPitch = newPitch;

    public async ValueTask<bool> Enable(params string[]? args)
    {
        var command = args!.First();

        switch (command)
        {
            case "enable_freecam":
            {
                _memory.ReplaceBytes(_cameraFunctionAddress, CustomCameraFunctionShellCode);
                
                if (!_memory.ReadValue(_cameraCoordinatesAddress, out _currentCameraPosition)
                    && _currentCameraPosition == Vector3.Zero)
                {
                    await Disable();

                    return false;
                }

                if (!_memory.ReadValueConstant<float>(_cameraPitchAddress,
                        RefreshPitch,
                        TimeSpan.FromMilliseconds(1)))
                {
                    await Disable();

                    return false;
                }

                if (!_memory.ReadValueConstant<float>(_cameraYawAddress,
                        RefreshYaw,
                        TimeSpan.FromMilliseconds(1)))
                {
                    await Disable();

                    return false;
                }

                break;
            }
            case "forward":
            {
                var newCoordinates = TrainerHelper.TeleportForward(_currentCameraPosition,
                    _currentYaw - 90f, _currentPitch, 20f);

                WriteNewCameraCoords(newCoordinates);

                break;
            }
            case "backward":
            {
                var newCoordinates = TrainerHelper.TeleportBackward(_currentCameraPosition,
                    _currentYaw - 90f, _currentPitch, 20f);

                WriteNewCameraCoords(newCoordinates);

                break;
            }
            case "up":
            {
                _currentCameraPosition.Z += 10f;

                WriteNewCameraCoords(_currentCameraPosition);

                break;
            }
            case "down":
            {
                _currentCameraPosition.Z -= 10f;

                WriteNewCameraCoords(_currentCameraPosition);

                break;
            }
            case "right":
            {
                var newCoordinates = TrainerHelper.TeleportForwardWithoutZ(_currentCameraPosition,
                    _currentYaw, 10f);

                WriteNewCameraCoords(newCoordinates);

                break;
            }
            case "left":
            {
                var newCoordinates = TrainerHelper.TeleportForwardWithoutZ(_currentCameraPosition,
                    _currentYaw - 180f, 20f);

                WriteNewCameraCoords(newCoordinates);

                break;
            }
        }

        return true;
    }

    public ValueTask<bool> Disable(params string[]? args)
    {
        _memory.StopReadingValueConstant(_cameraPitchAddress);
        _memory.StopReadingValueConstant(_cameraYawAddress);

        _memory.UndoReplaceBytes(_cameraFunctionAddress);
        
        return ValueTask.FromResult(true);
    }

    private void WriteNewCameraCoords(Vector3 newCoordinates)
    {
        _currentCameraPosition = newCoordinates;

        _memory.WriteValue(_cameraCoordinatesAddress, newCoordinates);
    }

    private void OnReInitializeTargetProcess()
    {
        _memory.StopReadingValueConstant(_cameraPitchAddress);
        _memory.StopReadingValueConstant(_cameraYawAddress);

        _currentCameraPosition = Vector3.Zero;
        _currentPitch = 0f;
        _currentYaw = 0f;
    }
}
```

---

## 🛠️ Planned / Community Features
- AOB & pattern scanning
- Pointer‑chain helpers
- Linux version in the future

---
### Contributions and ideas are very welcome, feel free to open an Issue or PR!