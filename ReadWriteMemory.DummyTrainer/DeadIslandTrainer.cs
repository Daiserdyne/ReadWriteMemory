using ReadWriteMemory.DummyTrainer.Dl2Trainer;
using ReadWriteMemory.Main;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.DummyTrainer;

internal sealed class DeadIslandTrainer
{
    public static async Task Main()
    {
        using var memory = TrainerServices.CreateAndSingletonInstance("DeadIsland-Win64-Shipping");

        memory.Process_OnStateChanged += (o) => { Console.WriteLine(o ? "Process is running" : "Process is not running"); };

        var enabled = false;

        var godmode = new Godmode();

        while (true)
        {
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F1))
            {
                enabled = !enabled;

                if (enabled)
                {
                    await godmode.Enable();
                }
                else
                {
                    await godmode.Disable();
                }
            }
        }
    }
}