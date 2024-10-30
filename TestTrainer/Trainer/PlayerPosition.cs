using System.Numerics;
using ReadWriteMemory;
using ReadWriteMemory.Entities;
using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Services;

namespace TestTrainer.Trainer;

public sealed class PlayerPosition : IMemoryTrainer
{
    private readonly RwMemory _memory = RwMemoryHelper.RwMemory;
    private readonly MemoryAddress _playerPosition = new(0x219FF58, "Outlast2.exe", 0x250, 0x88);

    private Vector3 _savedPlayerPosition = Vector3.Zero;
    
    private bool _displayingCoords;
    
    private CancellationTokenSource? _coordsMonitorSrc;

    public int Id { get; } = 0;
    public string TrainerName { get; } = nameof(PlayerPosition);

    public string Description { get; } = "Player teleportation.";

    public bool DisableWhenDispose { get; } = false;

    public Task Enable(params string[]? args)
    {
        switch (args![0])
        {
            case "SavePosition":
            {
                if (_memory.ReadValue(_playerPosition, out _savedPlayerPosition))
                {
                    Console.WriteLine(_savedPlayerPosition);
                }
                break;
            }

            case "LoadPosition":
            {
                if (_savedPlayerPosition != Vector3.Zero)
                {
                    _memory.WriteValue(_playerPosition, _savedPlayerPosition);
                }

                break;
            }

            case "DisplayPosition":
            {
                _displayingCoords = !_displayingCoords;
                
                if (_displayingCoords)
                {
                    _coordsMonitorSrc ??= new();
                    _memory.ReadValue<Vector3>(_playerPosition, PlayerCoords, TimeSpan.FromMilliseconds(250), _coordsMonitorSrc.Token);
                }
                else
                {
                    _coordsMonitorSrc?.Cancel();
                    _coordsMonitorSrc?.Dispose();
                    _coordsMonitorSrc = null;
                }
                
                break;
            }
        }

        return Task.CompletedTask;
    }

    private static void PlayerCoords(bool success, Vector3 coords)
    {
        if (success)
        {
            Console.WriteLine(coords);
        }
    }
    
    public Task Disable(params string[]? args)
    {
        return Task.CompletedTask;
    }
}