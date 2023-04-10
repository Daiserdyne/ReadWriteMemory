namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    internal static Task ExecuteTaskInfinite(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        Task.Run(async () =>
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

        return Task.CompletedTask;
    }
}