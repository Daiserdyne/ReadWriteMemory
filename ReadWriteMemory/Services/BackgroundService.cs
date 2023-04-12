namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    internal static async Task ExecuteTaskInfinite(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                taskToExecute.Invoke();
            }
            catch
            {
                break;
            }

            await Task.Delay(repeatTime, ct);
        }
    }
}