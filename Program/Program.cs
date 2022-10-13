using ReadWriteMemory;
using ReadWriteMemory.Models;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Program;

internal class Program
{
    //private static readonly MemoryAddress _health = new(0x219FF58, "Outlast2.exe", 0xc38, 0x7f58);

    private readonly static MemoryAddress _movementXAddress = new(0x56C55F, "Outlast2.exe");
    private readonly static MemoryAddress _movementYAddress = new(0x56C568, "Outlast2.exe");
    private readonly static MemoryAddress _XCoords = new(0x219FF58, "Outlast2.exe", 0x250, 0x88);
    private readonly static byte[] _movementX = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x17, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xEB, 0x15, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04 };
    private readonly static byte[] _movementY = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x12, 0x90, 0x90, 0x90, 0x90, 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x7B, 0x04 };

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);

    protected internal static async Task Main()
    {
        using Memory memory = Memory.Instance("Outlast2");

        memory.Logger.OnLogging += Logger_OnLogging;

        while (true)
        {
            switch (Console.ReadLine())
            {
                case "r":
                    var r = memory.ReadCoordinates(_XCoords);
                    Console.WriteLine($"X: {r.Value.X} Y: {r.Value.Y} Z: {r.Value.Z} ");
                    break;

                case "t":
                    memory.WriteCoordinates(_XCoords, new Vector3(-3746.308105f, 3277.897461f, -20000));
                    break;

                case "f":
                    memory.FreezeFloat(_XCoords, 1);
                    break;

                case "u":
                    memory.UnfreezeValue(_XCoords);
                    break;

                case "a":
                    _ = await memory.CreateOrResumeCodeCaveAsync(_movementXAddress, _movementX, 9);
                    _ = await memory.CreateOrResumeCodeCaveAsync(_movementYAddress, _movementY, 5);
                    break;

                case "d":
                    memory.PauseOpenedCodeCave(_movementXAddress);
                    memory.PauseOpenedCodeCave(_movementYAddress);
                    break;

                case "c":
                    memory.CloseCodeCave(_movementXAddress);
                    memory.CloseCodeCave(_movementYAddress);
                    break;

                case "exit":
                    memory.Logger.OnLogging -= Logger_OnLogging;
                    memory.Dispose();
                    return;

                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }

    private static void Logger_OnLogging(string caption, string message)
    {
        Console.WriteLine(message);
    }
}