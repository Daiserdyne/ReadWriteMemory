using ReadWriteMemory.DummyTrainer.Outlast2;
using ReadWriteMemory.Services;
using ReadWriteMemory.Utilities;

namespace ReadWriteMemory.DummyTrainer;

internal sealed class Outlast2Trainer
{
    public static async Task Main3()
    {
        using var memory = TrainerServices.CreateAndGetSingletonInstance("Outlast2");

        memory.Process_OnStateChanged += (o) => { Console.WriteLine(o ? "Process is running" : "Process is not running"); };

        var enabled = false;

        var freezeEnemies = new FreezeEnemies();

        while (true)
        {
            if (await Hotkeys.KeyPressedAsync(Hotkeys.Key.VK_F1))
            {
                enabled = !enabled;

                if (enabled)
                {
                    await freezeEnemies.Enable();
                    continue;
                }

                await freezeEnemies.Disable();
            }
        }
    }
}