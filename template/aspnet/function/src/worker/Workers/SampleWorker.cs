using OpenFaaS.Shared;

namespace OpenFaaS.Worker.Workers;

/// <summary>
/// Sample background worker using Task.Delay loop.
/// Replace this with your actual background job logic.
///
/// Toggle via environment variable: sample-worker-enabled = true/false
/// Interval via environment variable: sample-worker-interval-sec = 30
/// </summary>
public class SampleWorker(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<SampleWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSec = config.GetValue("sample-worker-interval-sec", 30);
        logger.LogInformation("SampleWorker started. Interval: {Interval}s. Shared version: {Version}",
            intervalSec, SharedInfo.Version);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), stoppingToken);

                using var scope = scopeFactory.CreateScope();
                // Resolve your scoped services here and do work
                logger.LogInformation("SampleWorker executed at {Time}", DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("SampleWorker cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SampleWorker failed");
            }
        }
    }
}
