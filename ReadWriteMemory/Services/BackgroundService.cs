namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    internal static async Task ExecuteTaskInfiniteAsync(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        await Task.Run(async () =>
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

                await Task.Delay(repeatTime);
            }
        }, ct);
    }
}