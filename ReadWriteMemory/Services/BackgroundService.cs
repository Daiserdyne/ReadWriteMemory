namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    private const double MaxFreezeRefreshRateInMilliseconds = double.MaxValue;
    private const ushort MinFreezeRefreshRateInMilliseconds = 5;

    internal static async Task ExecuteTaskInfinite(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        repeatTime = GetValidRefreshRate(repeatTime);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                taskToExecute.Invoke();
            }
            catch
            {
                throw;
            }

            await Task.Delay(repeatTime, ct);
        }
    }

    private static TimeSpan GetValidRefreshRate(TimeSpan freezeRefreshRate)
    {
        switch ((double)(long)freezeRefreshRate.TotalMilliseconds)
        {
            case < MinFreezeRefreshRateInMilliseconds:
                freezeRefreshRate = TimeSpan.FromMilliseconds(MinFreezeRefreshRateInMilliseconds);
                break;

            case > MaxFreezeRefreshRateInMilliseconds:
                freezeRefreshRate = TimeSpan.FromMilliseconds(MaxFreezeRefreshRateInMilliseconds);
                break;
        }

        return freezeRefreshRate;
    }
}