using ReadWriteMemory;
using ReadWriteMemory.Models;

namespace Program;

internal class Program
{
    private static readonly Memory _mem = Memory.Instance("Outlast2");
    private static readonly MemoryAddress _health = new(0x219FF58, "Outlast2.exe", 0xc38, 0x7f58);

    private readonly static MemoryAddress _movementXAddress = new(0x56C55F, "Outlast2.exe");
    private readonly static MemoryAddress _movementYAddress = new(0x56C568, "Outlast2.exe");
    private readonly static byte[] _movementX = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x17, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x58, 0x7B, 0x04, 0xEB, 0x15, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x33, 0xF3, 0x0F, 0x58, 0x7B, 0x04 };
    private readonly static byte[] _movementY = { 0x81, 0xBB, 0xE0, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74, 0x12, 0x90, 0x90, 0x90, 0x90, 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xF3, 0x0F, 0x11, 0x7B, 0x04 };

    protected internal static async Task Main()
    {
        _mem.Logger.OnLogging += Logger_OnLogging;


        while (true)
        {
            Console.WriteLine("Current health: " + _mem.ReadFloat(_health));

            switch (Console.ReadLine())
            {
                case "a":
                    _ = await _mem.CreateOrResumeCodeCaveAsync(_movementXAddress, _movementX, 9);
                    _ = await _mem.CreateOrResumeCodeCaveAsync(_movementYAddress, _movementY, 5);
                    break;

                case "d":
                    _mem.PauseOpenedCodeCave(_movementXAddress);
                    _mem.PauseOpenedCodeCave(_movementYAddress);
                    break;

                case "c":
                    _mem.CloseCodeCave(_movementXAddress);
                    _mem.CloseCodeCave(_movementYAddress);
                    break;

                case "exit":
                    _mem.Dispose();
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