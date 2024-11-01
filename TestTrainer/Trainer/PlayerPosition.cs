using System.Numerics;
using ReadWriteMemory;
using ReadWriteMemory.Entities;
using ReadWriteMemory.Interfaces;
using ReadWriteMemory.Services;

namespace TestTrainer.Trainer;

public sealed class PlayerPosition : IMemoryTrainer
{
    private readonly RwMemory _memory = RwMemoryHelper.RwMemory;
    private readonly MemoryAddress _playerPositionAddress = new(0x219FF58, "Outlast2.exe", 0x250, 0x88);

    private Vector3 _savedPlayerPosition = Vector3.Zero;
    private bool _displayingCoords;
    private bool _displayingCoordsAsBytes;
    private bool _freezePlayer;

    public PlayerPosition() => _memory.OnReinitilizeTargetProcess += OnReinitilizeTargetProcess;

    private void OnReinitilizeTargetProcess()
    {
        _savedPlayerPosition = Vector3.Zero;
        _displayingCoords = false;
        _displayingCoordsAsBytes = false;
        _freezePlayer = false;
    }

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
                if (_memory.ReadValue(_playerPositionAddress, out _savedPlayerPosition))
                {
                    Console.WriteLine(_savedPlayerPosition);
                }

                break;
            }

            case "LoadPosition":
            {
                if (_savedPlayerPosition != Vector3.Zero)
                {
                    _memory.WriteValue(_playerPositionAddress, _savedPlayerPosition);
                }

                break;
            }

            case "DisplayPosition":
            {
                _displayingCoords = !_displayingCoords;

                if (_displayingCoords)
                {
                    if (!_memory.ReadValueConstant<Vector3>(_playerPositionAddress, PlayerCoords,
                            TimeSpan.FromMilliseconds(250)))
                    {
                        Console.WriteLine("Read adress wird schon benutzt.");
                    }
                }
                else
                {
                    _memory.StopReadingValueConstant(_playerPositionAddress);
                }

                break;
            }

            case "DisplayPositionAsBytes":
            {
                _displayingCoordsAsBytes = !_displayingCoordsAsBytes;

                if (_displayingCoordsAsBytes)
                {
                    if (!_memory.ReadBytesConstant(_playerPositionAddress, 12, PlayerCoordsBytes,
                            TimeSpan.FromMilliseconds(250)))
                    {
                        Console.WriteLine("Read adress wird schon benutzt.");
                    }
                }
                else
                {
                    _memory.StopReadingValueConstant(_playerPositionAddress);
                }

                break;
            }

            case "FreezePlayer":
            {
                _freezePlayer = !_freezePlayer;

                if (_freezePlayer)
                {
                    _memory.FreezeValue<Vector3>(_playerPositionAddress, TimeSpan.FromMilliseconds(5));
                }
                else
                {
                    _memory.UnfreezeValue(_playerPositionAddress);
                }

                break;
            }
        }

        return Task.CompletedTask;
    }

    private static void PlayerCoordsBytes(byte[] coords)
    {
        foreach (var coord in coords)
        {
            Console.Write(coord);
        }

        Console.WriteLine();
    }

    private static void PlayerCoords(Vector3 coords)
    {
        Console.WriteLine(coords);
    }

    public Task Disable(params string[]? args)
    {
        return Task.CompletedTask;
    }
}