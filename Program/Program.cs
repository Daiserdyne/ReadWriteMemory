using Pastel;
using Program.Trainer;
using ReadWriteMemory;
using ReadWriteMemory.Hotkeys;
using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using ReadWriteMemory.Services;
using ReadWriteMemory.Trainer.Interface;
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

    private readonly static MemoryAddress _noCollisionX = new(0xEF3113, "Outlast2.exe");
    private readonly static MemoryAddress _noCollisionY = new(0xEF3119, "Outlast2.exe");

    private static readonly Memory memory = new("Outlast2");

    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        memory.Logger.MemoryLogger_OnLogging -= Logger_MemoryLogger_OnLogging;
        memory.Dispose();
    }

    protected internal static async Task Main()
    {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        memory.Logger.MemoryLogger_OnLogging += Logger_MemoryLogger_OnLogging;

        memory.Process_OnStateChanged += Memory_Process_OnStateChanged;

        var trainer = new Dictionary<string, ITrainer>()
        {
            { nameof(FreezeAllEnemies), new FreezeAllEnemies(memory) }
        };

        while (true)
        {
            if (await Hotkeys.HotKeyPressedAsync(Hotkeys.Hotkey.VK_F1))
            {
                Console.WriteLine("F1");
            }
            if (await Hotkeys.HotKeyPressedAsync(0x71, false))
            {
                Console.WriteLine("F2");
            }

            Thread.Sleep(1);
        }

        //while (true)
        //{
        //    switch (Console.ReadLine())
        //    {
        //        case "readfloat":
        //            if (memory.ReadMemory(_XCoords, Memory.MemoryDataTypes.Float, out var value))
        //                Console.WriteLine(value);
        //            break;

        //        case "z":
        //            memory.WriteMemory(_XCoords, -4054.958008f);
        //            break;

        //        case "dn":
        //            memory.WriteMemory(_noCollisionX, new byte[] { 0xFF, 0x90, 0xE8, 0x0A, 0x00, 0x00 });
        //            memory.WriteMemory(_noCollisionY, new byte[] { 0xE9, 0x69, 0x16, 0x00, 0x00 });
        //            break;

        //        case "n":
        //            memory.WriteMemory(_noCollisionX, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
        //            memory.WriteMemory(_noCollisionY, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });
        //            break;

        //        case "r":
        //            if (memory.ReadFloatCoordinates(_XCoords, out var coordinates))
        //                Console.WriteLine($"X: {coordinates.X} Y: {coordinates.Y} Z: {coordinates.Z}");

        //            break;

        //        case "t":
        //            memory.WriteFloatCoordinates(_XCoords, new Vector3(-3746.308105f, 3277.897461f, -20000));
        //            break;

        //        case "f":
        //            await trainer["FreezeAllEnemies"].Enable();
        //            break;

        //        case "u":
        //            await trainer["FreezeAllEnemies"].Disable();
        //            break;

        //        case "a":
        //            _ = await memory.CreateOrResumeCodeCaveAsync(_movementXAddress, _movementX, 9);
        //            _ = await memory.CreateOrResumeCodeCaveAsync(_movementYAddress, _movementY, 5);
        //            break;

        //        case "d":
        //            memory.PauseOpenedCodeCave(_movementXAddress);
        //            memory.PauseOpenedCodeCave(_movementYAddress);
        //            break;

        //        case "c":
        //            memory.CloseCodeCave(_movementXAddress);
        //            memory.CloseCodeCave(_movementYAddress);
        //            break;

        //        case "exit":
        //            memory.Logger.MemoryLogger_OnLogging -= Logger_MemoryLogger_OnLogging;
        //            memory.Dispose();
        //            return;

        //        default:
        //            Console.WriteLine("Unknown command.");
        //            break;
        //    }
        //}
    }

    private static void Memory_Process_OnStateChanged(bool newProcessState)
    {
        Console.WriteLine(newProcessState);
    }

    private static async void Logger_MemoryLogger_OnLogging(MemoryLogger.LoggingType type, string message)
    {
        await Task.Run(() =>
        {
            switch (type)
            {
                case MemoryLogger.LoggingType.Info:
                case MemoryLogger.LoggingType.Warn:
                case MemoryLogger.LoggingType.Error:
                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString().Pastel("#5D599C")}][{type.ToString().Pastel("#3E7B4B")}]" +
                        $"{" ->".Pastel("#53859C")} {message}");
                    break;
            }
        });
    }
}