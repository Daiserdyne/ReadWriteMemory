using System.Numerics;
using ReadWriteMemory.Internal;
using ReadWriteMemory.Internal.Entities;
using ReadWriteMemory.Internal.NativeImports;
using ReadWriteMemory.Internal.Utilities;

namespace TestTrainer.Internal.InjectMe;

public static class TrialsTrainer
{
    private static readonly MemoryAddress _cameraCoordinatesAddress =
        new("TOTClient-Win64-Shipping.exe", 0x5DE5A50,
            0x218, 0x3A8, 0x2A0, 0x1E0);

    private static readonly MemoryAddress _cameraPitchAddress =
        new("TOTClient-Win64-Shipping.exe", 0x5E9FAD0,
            0x30, 0x260, 0x2A0, 0x6C0, 0x68, 0x430, 0x74);

    private static readonly MemoryAddress _cameraYawAddress =
        new("TOTClient-Win64-Shipping.exe", 0x5DE5A50,
            0x208, 0x870, 0x20, 0x29C);

    private static readonly RwMemory _memory = new();

    internal static async Task Main(CancellationToken token)
    {
        Kernel32.AllocConsole();
        
        while (!token.IsCancellationRequested)
        {
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.F4))
            {
                var result = _memory.ReadValueConstant(_cameraCoordinatesAddress,
                    (Vector3 coords) => { Console.WriteLine($"coords: {coords}\n"); }, TimeSpan.FromMilliseconds(1000));

                Console.WriteLine(result);

                result = _memory.ReadValueConstant(_cameraPitchAddress,
                    (float pitch) => { Console.WriteLine($"pitch: {pitch}\n"); }, TimeSpan.FromMilliseconds(1000));

                Console.WriteLine(result);

                result = _memory.ReadValueConstant(_cameraYawAddress,
                    (float yaw) => { Console.WriteLine($"yaw: {yaw}\n"); }, TimeSpan.FromMilliseconds(1000));

                Console.WriteLine(result);
            }

            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.F5))
            {
                var result = _memory.StopReadingValueConstant(_cameraCoordinatesAddress);

                Console.WriteLine(result);

                result = _memory.StopReadingValueConstant(_cameraPitchAddress);

                Console.WriteLine(result);

                result = _memory.StopReadingValueConstant(_cameraYawAddress);

                Console.WriteLine(result);
            }

            await Task.Delay(1, token);
        }
    }
}