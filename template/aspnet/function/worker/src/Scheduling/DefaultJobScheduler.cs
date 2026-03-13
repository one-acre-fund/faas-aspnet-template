namespace OpenFaaS.Worker.Scheduling;

/// <summary>
/// Default scheduler that uses simple Task.Delay loops.
/// Replace with QuartzScheduler or HangfireScheduler when you need
/// cron expressions, persistent stores, or a dashboard.
/// </summary>
public class DefaultJobScheduler : IJobScheduler
{
    public Task ScheduleRecurringAsync<TJob>(string intervalOrCron, CancellationToken ct)
        where TJob : IScheduledJob
    {
        // In the default implementation, scheduling is handled directly by
        // BackgroundService subclasses using Task.Delay loops.
        // This method is a no-op placeholder for API consistency.
        return Task.CompletedTask;
    }
}
