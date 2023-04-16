using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using System.Diagnostics;
using Windows.UI.Input;

namespace ReadWriteMemory.DummyTrainer;

internal sealed class DummyTrainer
{
    private readonly static MemoryAddress _movementXAddress = new(0x56C55F, "Outlast2.exe");
    private readonly static MemoryAddress _movementYAddress = new(0x56C568, "Outlast2.exe");
    private readonly static byte[] _movementX = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x17, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xEB, 0x15, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04 };
    private readonly static byte[] _movementY = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x12, 0x90, 0x90, 0x90, 0x90, 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x7B, 0x04 };

    private readonly static MemoryAddress _x = new(0x219FF58, "Outlast2.exe", 0x250, 0x88);

    private readonly static MemoryAddress _hp = new(0x219FF58, "Outlast2.exe", 0xC38, 0x7F58);

    internal static async Task Main()
    {
        var memory = new Memory("Outlast2");

        var stopwatch = new Stopwatch();

        while (true)
        {
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F1))
            {
                stopwatch.Start();

                _ = await memory.CreateOrResumeCodeCaveAsync(_movementXAddress, _movementX, 9);
                _ = await memory.CreateOrResumeCodeCaveAsync(_movementYAddress, _movementY, 5);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("CreateOrResumeCodeCaveAsync took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F2))
            {
                stopwatch.Start();

                memory.PauseOpenedCodeCave(_movementXAddress);
                memory.PauseOpenedCodeCave(_movementYAddress);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("PauseOpenedCodeCave took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F3))
            {
                stopwatch.Start();

                memory.CloseCodeCave(_movementXAddress);
                memory.CloseCodeCave(_movementYAddress);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("CloseCodeCave took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F4))
            {
                stopwatch.Start();

                memory.ChangeAndFreezeValue(_hp, 1f);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("ChangeAndFreezeValue took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F5))
            {
                stopwatch.Start();

                memory.UnfreezeValue(_hp);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("UnfreezeValue took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F6))
            {
                stopwatch.Start();

                memory.WriteProcessMemory<float>(_hp, 5);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("WriteProcessMemory took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F7))
            {
                stopwatch.Start();

                memory.ReadProcessMemory(_hp, Memory.MemoryDataTypes.Float, out var value);

                await Console.Out.WriteLineAsync("ReadProcessMemory took " + value?.ToString());

                stopwatch.Stop();

                stopwatch.Reset();
            }
            else if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F8))
            {
                stopwatch.Start();

                memory.ReadFloatCoordinates(_x, out var coords);

                await Console.Out.WriteLineAsync($"X: {coords.X}, Y: {coords.Y}, Z: {coords.Z}");

                stopwatch.Stop();

                await Console.Out.WriteLineAsync(stopwatch.ElapsedMilliseconds + "ms");

                stopwatch.Reset();
            }


            await Task.Delay(10);
        }
    }
}