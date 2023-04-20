using ReadWriteMemory.Main;
using ReadWriteMemory.Models;
using ReadWriteMemory.Utilities;
using System.Diagnostics;

namespace ReadWriteMemory.DummyTrainer;

internal sealed class DummyTrainer
{
    private readonly static MemoryAddress _movementXAddress = new(0x56C55F, "Outlast2.exe");
    private readonly static MemoryAddress _movementYAddress = new(0x56C568, "Outlast2.exe");
    private readonly static byte[] _movementX = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x17, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xEB, 0x15, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04 };
    private readonly static byte[] _movementY = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x12, 0x90, 0x90, 0x90, 0x90, 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x7B, 0x04 };

    private readonly static MemoryAddress _x = new(0x219FF58, "Outlast2.exe", 0x250, 0x88);

    private readonly static MemoryAddress _hp = new(0x219FF58, "Outlast2.exe", 0xC38, 0x7F58);

    private readonly static MemoryAddress _STRING = new(0x2A731633A10);

    internal static async Task Main()
    {
        using var memory = new RWMemory("Outlast2");

        var stopwatch = new Stopwatch();

        var test = new CancellationTokenSource();   

        bool enabled = false;

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
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F2))
            {
                stopwatch.Start();

                memory.PauseOpenedCodeCave(_movementXAddress);
                memory.PauseOpenedCodeCave(_movementYAddress);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("PauseOpenedCodeCave took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F3))
            {
                stopwatch.Start();

                memory.CloseCodeCave(_movementXAddress);
                memory.CloseCodeCave(_movementYAddress);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("CloseCodeCave took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F4))
            {
                stopwatch.Start();

                memory.ChangeAndFreezeValue(_hp, 5f, TimeSpan.FromSeconds(1));

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("ChangeAndFreezeValue took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F5))
            {
                stopwatch.Start();

                memory.UnfreezeValue(_hp);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("UnfreezeValue took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F6))
            {
                stopwatch.Start();

                memory.WriteValue<float>(_hp, 5);

                stopwatch.Stop();

                await Console.Out.WriteLineAsync("WriteProcessMemory took " + stopwatch.ElapsedMilliseconds.ToString());

                stopwatch.Reset();
            }
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F7))
            {
                enabled = enabled ? false : true;

                if (enabled)
                {
                    memory.ReadValue<Coordinates>(_x, ReadValue, TimeSpan.FromMilliseconds(10), test.Token);
                    continue;
                }

                test.Cancel();
                test = new();
            }

            await Task.Delay(5);
        }
    }

    public static void ReadValue<T>(bool success, T value)
    {
        if (success)
        {
            Console.Clear();
            Console.WriteLine(value);
        }
    }

    public struct Coordinates
    {
        public float X, Y, Z;

        public override string ToString()
        {
            return $"X: {X}\nY: {Y}\nZ: {Z}\n";
        }
    }
}