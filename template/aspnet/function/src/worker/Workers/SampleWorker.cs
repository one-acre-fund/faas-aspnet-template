using OpenFaaS.Shared;

namespace OpenFaaS.Worker.Workers;

/// <summary>
/// Sample Hangfire recurring job.
/// Replace this with your actual background job logic.
///
/// Interval via environment variable: sample-worker-interval-sec = 30
/// </summary>
public class SampleWorker(ILogger<SampleWorker> logger)
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("SampleWorker executed at {Time}. Shared version: {Version}",
            DateTime.UtcNow, SharedInfo.Version);

        return Task.CompletedTask;
    }
}
