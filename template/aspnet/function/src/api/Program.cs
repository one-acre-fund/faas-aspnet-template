using System.Diagnostics.CodeAnalysis;
using OpenFaaS.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add your services here
// builder.Services.AddSingleton<IMyService, MyService>();

var app = builder.Build();

app.MapGet("/", () => new
{
    Message = "Hello from the API",
    SharedVersion = SharedInfo.Version
});

app.MapPost("/", () => new
{
    Message = "Hello from the API",
    SharedVersion = SharedInfo.Version
});

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program;
