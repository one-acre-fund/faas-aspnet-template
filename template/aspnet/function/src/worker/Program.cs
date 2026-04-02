using System.Diagnostics.CodeAnalysis;
using Hangfire;
using Hangfire.InMemory;
using OpenFaaS.Worker.Workers;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// ── Hangfire ──
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddHangfireServer();

var app = builder.Build();

// ── Schedule recurring jobs ──
app.Lifetime.ApplicationStarted.Register(() =>
{
    var intervalSec = config.GetValue("sample-worker-interval-sec", 30);
    RecurringJob.AddOrUpdate<SampleWorker>(
        "sample-worker",
        job => job.ExecuteAsync(CancellationToken.None),
        $"*/{Math.Max(1, intervalSec / 60)} * * * *");
});

// Minimal health-check endpoint required by of-watchdog
app.MapGet("/", () => Results.Ok(new { Status = "healthy", Service = "worker" }));
app.MapPost("/", () => Results.Ok(new { Status = "healthy", Service = "worker" }));

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program;
