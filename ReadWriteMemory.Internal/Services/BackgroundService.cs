namespace ReadWriteMemory.Internal.Services;

internal static class BackgroundService
{
    private const double MaxFreezeRefreshRateInMilliseconds = double.MaxValue;
    private const byte MinFreezeRefreshRateInMilliseconds = 1;

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
        freezeRefreshRate = (double)(long)freezeRefreshRate.TotalMilliseconds switch
        {
            < MinFreezeRefreshRateInMilliseconds => TimeSpan.FromMilliseconds(MinFreezeRefreshRateInMilliseconds),
            > MaxFreezeRefreshRateInMilliseconds => TimeSpan.FromMilliseconds(MaxFreezeRefreshRateInMilliseconds),
            _ => freezeRefreshRate
        };

        return freezeRefreshRate;
    }
}