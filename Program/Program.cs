using Pastel;
using Program.Trainer;
using ReadWriteMemory;
using ReadWriteMemory.Hotkeys;
using ReadWriteMemory.Logging;
using ReadWriteMemory.Models;
using ReadWriteMemory.Trainer.Interface;

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

    private static void OnApplicationExit(object? sender, EventArgs e)
    {
        memory.Logger.MemoryLogger_OnLogging -= Logger_MemoryLogger_OnLogging;
        memory.Dispose();

    }

    protected internal static async Task Main()
    {
        //// Erstelle eine Quaternion-Rotation
        //Quaternion rotation = Quaternion.CreateFromYawPitchRoll(
        //    (float)(Math.PI / -178.2587128f), // 30 Grad in Bogenmaß
        //    (float)0, // -15 Grad in Bogenmaß
        //    0f);

        //// Erstelle eine aktuelle Position
        //Vector3 currentPosition = new Vector3(1406.96228f, -19125f, -1801.138916f);

        //// Definiere eine Distanz
        //float distance = 400f;

        //// Berechne die neue Position des Objekts
        //Vector3 newPosition = CalculateNewPosition(rotation, currentPosition, distance);

        //// Gib die neue Position auf der Konsole aus
        //Console.WriteLine($"Neue Position: {newPosition}");


        AppDomain.CurrentDomain.ProcessExit += OnApplicationExit;

        memory.Logger.MemoryLogger_OnLogging += Logger_MemoryLogger_OnLogging;

        memory.Process_OnStateChanged += Memory_Process_OnStateChanged;

        var trainer = new Dictionary<string, ITrainer>()
        {
            { nameof(FreezeAllEnemies), new FreezeAllEnemies(memory) }
        };

        bool freezeEnemies = false;

        while (true)
        {
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F1))
            {
                if (memory.ReadMemory(_XCoords, Memory.MemoryDataTypes.Float, out var value))
                    Console.WriteLine(value);
            }
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F2))
            {
                freezeEnemies = !freezeEnemies;

                if (freezeEnemies)
                {
                    _ = await memory.CreateOrResumeCodeCaveAsync(_movementXAddress, _movementX, 9);
                    _ = await memory.CreateOrResumeCodeCaveAsync(_movementYAddress, _movementY, 5);
                }
                else
                {
                    memory.PauseOpenedCodeCave(_movementXAddress);
                    memory.PauseOpenedCodeCave(_movementYAddress);
                }
            }

            Thread.Sleep(1);
        }
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