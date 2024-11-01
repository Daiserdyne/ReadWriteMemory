namespace ReadWriteMemory.Services;

internal static class BackgroundService
{
    private const double MaxFreezeRefreshRateInMilliseconds = double.MaxValue;
    private const ushort MinFreezeRefreshRateInMilliseconds = 1;

    internal static async Task ExecuteTaskRepeatedly(Action taskToExecute, TimeSpan repeatTime, CancellationToken ct)
    {
        repeatTime = GetValidRefreshRate(repeatTime);

        while (!ct.IsCancellationRequested)
        {
            taskToExecute.Invoke();

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