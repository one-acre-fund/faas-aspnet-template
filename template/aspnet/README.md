# OpenFaaS ASPNET Multi-Project Template

A template for OpenFaaS that supports **multiple deployable projects** under a single solution with a shared library and a worker service.

## Installing

```bash
faas-cli template pull https://github.com/one-acre-fund/faas-aspnet-template#multi-project
```

## Template Structure

When you use this template, your project scaffold looks like this:

```
my-project/
├── stack.yml                          # Single OpenFaaS stack — all functions defined here
├── src/
│   ├── shared/shared.csproj           # Shared library (models, services, utilities)
│   ├── api/
│   │   ├── function.csproj            # HTTP API (references shared)
│   │   └── Program.cs
│   └── worker/
│       ├── worker.csproj              # Background worker (references shared)
│       ├── Program.cs
│       ├── Workers/SampleWorker.cs
│       └── Scheduling/IJobScheduler.cs
├── tests/
│   ├── unit/unit.csproj
│   └── integration/integration.csproj
├── .dockerignore
└── function.sln                       # Single solution, all projects
```

## How It Works

### `stack.yml`

All functions are defined in a single `stack.yml` at the project root. Setting `handler: .` tells `faas-cli` to use the project root as the Docker build context, so the Dockerfile sees the full source tree — shared library, all sub-projects, and solution.

### `PUBLISH_PROJECT` build arg

The Dockerfile uses a single `PUBLISH_PROJECT` build arg to select which sub-project to publish. Each function in `stack.yml` sets it:

```yaml
functions:
    my-project-api:
        lang: aspnet-multi
        handler: .
        build_args:
            PUBLISH_PROJECT: "api/src/function.csproj"
```

### `COPY --parents` + `.dockerignore`

The Dockerfile uses `COPY --parents function/*/src/*.csproj ./` with glob patterns — no hardcoded folder names. A `.dockerignore` at the project root excludes `bin/`, `obj/`, and build artifacts. **Adding a new sub-project requires zero Dockerfile changes.**

## Usage

### 1. Create Your Project

Set up your project directory and copy the scaffold, or use the template after pulling:

```bash
faas-cli new --lang aspnet-multi my-project
```

### 2. Rename and Customize

- Update function names, images, env vars, and secrets in `stack.yml`
- Add your code to `api/src/`, `worker/src/`, and `shared/src/`

### 3. Add More Sub-Projects

To add another deployable (e.g. a consumer):

1. Create `consumer/src/function.csproj` with a `ProjectReference` to `../../shared/src/shared.csproj`
2. Add a new function entry in `stack.yml` with `handler: .` and `build_args: { PUBLISH_PROJECT: "consumer/src/function.csproj" }`
3. Add the project to `function.sln`
4. Build and deploy — the template automatically parses your `.csproj` name to boot the correct DLL.

### 4. Build

```bash
cd my-project

# Build all functions
faas-cli build -f stack.yml

# Build a single function
faas-cli build -f stack.yml --filter my-project-api

# Push all
faas-cli push -f stack.yml

# Deploy all
faas-cli deploy -f stack.yml
```

## Worker Service

The worker is a standard ASP.NET app with `BackgroundService` classes. It exposes a minimal HTTP endpoint for OpenFaaS health checks, but its real work runs in hosted services.

### Toggling Workers

Each worker can be enabled/disabled via environment variables:

```yaml
environment:
    sample-worker-enabled: "true"
    sample-worker-interval-sec: "30"
```

### Scheduler Abstraction

The template includes an `IJobScheduler` interface for swapping scheduling backends:

| `scheduler-type` | Implementation | Package |
|---|---|---|
| `default` | `Task.Delay` loops (BackgroundService) | Built-in |
| `quartz` | Quartz.NET | Add `Quartz` NuGet |
| `hangfire` | Hangfire | Add `Hangfire` NuGet |

### Scaling

Pin the worker to exactly 1 replica to avoid duplicate job execution:

```yaml
labels:
    com.openfaas.scale.min: "1"
    com.openfaas.scale.max: "1"
```

## BuildKit

This template requires Docker BuildKit (for `COPY --parents` and secret mounts). Docker 23.0+ has BuildKit enabled by default. On older versions, enable it manually:

```bash
export DOCKER_BUILDKIT=1
```

## Private NuGet Feeds

Same as the standard `aspnet` template — use BuildKit secrets:

```bash
faas-cli build -f stack.yml --filter my-project-api --shrinkwrap
docker build --secret id=nuget.config,src=~/.nuget/NuGet/NuGet.Config build/my-project-api
```
