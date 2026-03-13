namespace OpenFaaS.Worker.Scheduling;

/// <summary>
/// Abstraction for job scheduling. Swap implementations via configuration:
/// - "default": BackgroundService with Task.Delay loops
/// - "quartz":  Quartz.NET (add Quartz NuGet package)
/// - "hangfire": Hangfire (add Hangfire NuGet package)
/// </summary>
public interface IJobScheduler
{
    Task ScheduleRecurringAsync<TJob>(string intervalOrCron, CancellationToken ct)
        where TJob : IScheduledJob;
}

public interface IScheduledJob
{
    Task ExecuteAsync(CancellationToken ct);
}
