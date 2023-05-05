namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    private const double MaxFreezeRefreshRateInMilliseconds = double.MaxValue;
    private const short MinFreezeRefreshRateInMilliseconds = 5;

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
        switch (Math.Round(freezeRefreshRate.TotalMilliseconds, 0))
        {
            case < MinFreezeRefreshRateInMilliseconds:
                freezeRefreshRate = TimeSpan.FromMilliseconds(MinFreezeRefreshRateInMilliseconds);
                break;

            case > double.MaxValue:
                freezeRefreshRate = TimeSpan.FromMilliseconds(MaxFreezeRefreshRateInMilliseconds);
                break;
        }

        return freezeRefreshRate;
    }
}