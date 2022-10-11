namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    public static async void ExecuteTaskAsync(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        await Task.Factory.StartNew(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                taskToExecute.Invoke();

                Thread.Sleep(repeatTime);
            }
        }, ct);
    }
}