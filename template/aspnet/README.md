# OpenFaaS ASPNET Multi-Project Template

A template for OpenFaaS that supports **multiple deployable projects** under a single solution with a shared library, a worker service, and a single test project.

## Installing

```bash
faas-cli template pull https://github.com/one-acre-fund/faas-aspnet-template#multi-project
```

## Template Structure

When you use this template, your project scaffold looks like this:

```
my-project/
├── shared/src/shared.csproj           # Shared library (models, services, utilities)
├── api/
│   ├── src/function.csproj            # HTTP API (references shared)
│   ├── src/Program.cs
│   └── my-project-api.yml             # OpenFaaS YAML — deployed independently
├── worker/
│   ├── src/function.csproj            # Background worker (references shared)
│   ├── src/Program.cs
│   ├── src/Workers/SampleWorker.cs
│   ├── src/Scheduling/IJobScheduler.cs
│   └── my-project-worker.yml          # OpenFaaS YAML — deployed independently
├── tests/
│   ├── unit/unit.csproj
│   └── integration/integration.csproj
├── .dockerignore
└── function.sln                       # Single solution, all projects
```

## How It Works

### `handler: ..`

Each sub-project's `.yml` lives inside its own folder (e.g. `api/`). Setting `handler: ..` tells `faas-cli` to use the **parent directory** (the project root) as the Docker build context. This means the Dockerfile sees the full source tree — shared library, all sub-projects, tests, and solution.

### `PUBLISH_PROJECT` build arg

The Dockerfile uses a single `PUBLISH_PROJECT` build arg to select which sub-project to publish. Each `.yml` overrides it:

```yaml
functions:
    my-project-api:
        lang: aspnet-multi
        handler: ..
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

- Rename the sample `.yml` files (e.g. `my-project-api.yml` → `payment-gateway-wrapper.yml`)
- Update the function name, image, env vars, and secrets in each `.yml`
- Add your code to `api/src/`, `worker/src/`, and `shared/src/`

### 3. Add More Sub-Projects

To add another deployable (e.g. a consumer):

1. Create `consumer/src/function.csproj` with a `ProjectReference` to `../../shared/src/shared.csproj`
2. Add a `consumer/my-project-consumer.yml` with `handler: ..` and `build_args: { PUBLISH_PROJECT: "consumer/src/function.csproj" }`
3. Add the project to `function.sln`
4. Deploy using `faas-cli build -f consumer/my-project-consumer.yml`, push, and deploy. The template automatically parses your `.csproj` name to boot the correct DLL.

### 4. Build

```bash
cd my-project

# Build individual sub-projects
faas-cli build -f api/my-project-api.yml
faas-cli build -f worker/my-project-worker.yml

# Push
faas-cli push -f api/my-project-api.yml
faas-cli push -f worker/my-project-worker.yml
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

This template requires Docker BuildKit (for `COPY --parents` and secret mounts). Enable it:

```bash
export DOCKER_BUILDKIT=1
```

## Private NuGet Feeds

Same as the standard `aspnet` template — use BuildKit secrets:

```bash
faas-cli build -f api/my-project-api.yml --shrinkwrap
docker build --secret id=nuget.config,src=~/.nuget/NuGet/NuGet.Config build/my-project-api
```
