namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    internal static async Task ExecuteTaskInfiniteAsync(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        await Task.Factory.StartNew(() =>
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

                Thread.Sleep(repeatTime);
            }
        }, ct);
    }
}