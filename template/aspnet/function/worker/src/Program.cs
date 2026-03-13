using System.Diagnostics.CodeAnalysis;
using OpenFaaS.Worker.Scheduling;
using OpenFaaS.Worker.Workers;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// ── Register scheduler ──
var schedulerType = config.GetValue<string>("scheduler-type") ?? "default";
switch (schedulerType)
{
    // case "quartz":   builder.Services.AddQuartzScheduler(config); break;
    // case "hangfire": builder.Services.AddHangfireScheduler(config); break;
    default: builder.Services.AddSingleton<IJobScheduler, DefaultJobScheduler>(); break;
}

// ── Register workers (toggle via config) ──
if (config.GetValue("sample-worker-enabled", true))
    builder.Services.AddHostedService<SampleWorker>();

var app = builder.Build();

// Minimal health-check endpoint required by of-watchdog
app.MapGet("/", () => Results.Ok(new { Status = "healthy", Service = "worker" }));
app.MapPost("/", () => Results.Ok(new { Status = "healthy", Service = "worker" }));

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program;
