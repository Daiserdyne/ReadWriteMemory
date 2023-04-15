using ReadWriteMemory.Models;
using System.Diagnostics;

namespace ReadWriteMemory.DummyTrainer;

internal sealed class DummyTrainer
{
    private readonly static MemoryAddress _movementXAddress = new(0x56C55F, "Outlast2.exe");
    private readonly static MemoryAddress _movementYAddress = new(0x56C568, "Outlast2.exe");
    private readonly static byte[] _movementX = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x17, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xEB, 0x15, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04 };
    private readonly static byte[] _movementY = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x12, 0x90, 0x90, 0x90, 0x90, 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x7B, 0x04 };

    internal static async Task Main()
    {
        var memory = new Memory("Outlast2");

        var stopwatch = new Stopwatch();

        while (true)
        {
            if (await Hotkeys.Hotkeys.KeyPressedAsync(Hotkeys.Hotkeys.Key.VK_F1))
            {
                stopwatch.Start();

                _ = await memory.CreateOrResumeCodeCaveAsync(_movementXAddress, _movementX, 9);
                _ = await memory.CreateOrResumeCodeCaveAsync(_movementYAddress, _movementY, 5);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("Creating code cave took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

                stopwatch.Reset();
            }

            if (await Hotkeys.Hotkeys.KeyPressedAsync(Hotkeys.Hotkeys.Key.VK_F2))
            {
                stopwatch.Start();

                memory.PauseOpenedCodeCave(_movementXAddress);
                memory.PauseOpenedCodeCave(_movementYAddress);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync(stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }

            if (await Hotkeys.Hotkeys.KeyPressedAsync(Hotkeys.Hotkeys.Key.VK_F3))
            {
                stopwatch.Start();

                memory.CloseCodeCave(_movementXAddress);
                memory.CloseCodeCave(_movementYAddress);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync(stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }

            await Task.Delay(10);
        }
    }
}