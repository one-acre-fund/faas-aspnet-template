# OpenFaaS ASPNET Multi-Project Template

A template for OpenFaaS that supports **multiple deployable projects** under a single solution with a shared library and a worker service.

## Installing

```bash
faas-cli template pull https://github.com/one-acre-fund/faas-aspnet-template#multi-project
```

## Getting Started

### 1. Pull the template and scaffold

```bash
mkdir my-project && cd my-project
faas-cli template pull https://github.com/one-acre-fund/faas-aspnet-template#net9-multi
faas-cli new my-project --lang aspnet
```

### 2. Flatten the project structure

`faas-cli new` nests the project files inside a `my-project/` subfolder. Move everything to the root:

```bash
mv my-project/* .
rm -rf my-project
```

### 3. Set up the stack file

The template includes a `stack.yml` with the full multi-project format (multiple functions, `build_args`, resource limits, etc.). Replace the auto-generated `my-project.yml` with it:

```bash
mv stack.yml my-project.yml
```

Your project should now look like this:

```
my-project/
├── my-project.yml
├── function.sln
├── src/
│   ├── api/
│   ├── shared/
│   └── worker/
├── tests/
│   ├── unit/
│   └── integration/
├── .gitignore
└── template/
```

### 4. Rename and customize

- Update function names, images, env vars, and secrets in `my-project.yml`
- Add your code to `src/api/`, `src/worker/`, and `src/shared/`

## Project Structure

```
my-project/
├── my-project.yml                     # OpenFaaS stack — all functions defined here
├── src/
│   ├── shared/shared.csproj           # Shared library (models, services, utilities)
│   ├── api/
│   │   ├── function.csproj            # HTTP API (references shared)
│   │   └── Program.cs
│   └── worker/
│       ├── worker.csproj              # Background worker (references shared)
│       ├── Program.cs
│       └── Workers/SampleWorker.cs
├── tests/
│   ├── unit/unit.csproj
│   └── integration/integration.csproj
├── .dockerignore
└── function.sln                       # Single solution, all projects
```

## How It Works

### Stack file

All functions are defined in a single stack file (e.g. `my-project.yml`) at the project root. Setting `handler: .` tells `faas-cli` to use the project root as the Docker build context, so the Dockerfile sees the full source tree — shared library, all sub-projects, and solution.

### `PUBLISH_PROJECT` build arg

The Dockerfile uses a single `PUBLISH_PROJECT` build arg to select which sub-project to publish. Each function in the stack file sets it:

```yaml
functions:
    my-project-api:
        lang: aspnet
        handler: .
        build_args:
            PUBLISH_PROJECT: "src/api/function.csproj"
```

### `COPY --parents` + `.dockerignore`

The Dockerfile uses `COPY --parents function/src/*/*.csproj ./` with glob patterns — no hardcoded folder names. A `.dockerignore` at the project root excludes `bin/`, `obj/`, and build artifacts. **Adding a new sub-project requires zero Dockerfile changes.**

## Adding More Sub-Projects

To add another deployable (e.g. a consumer):

1. Create `src/consumer/consumer.csproj` with a `ProjectReference` to `..\shared\shared.csproj`
2. Add a new function entry in your stack file with `handler: .` and `build_args: { PUBLISH_PROJECT: "src/consumer/consumer.csproj" }`
3. Add the project to `function.sln`
4. Build and deploy — the template automatically parses your `.csproj` name to boot the correct DLL.

## Building and Deploying

```bash
# Build all functions
faas-cli build -f my-project.yml

# Build a single function
faas-cli build -f my-project.yml --filter my-project-api

# Push all
faas-cli push -f my-project.yml

# Deploy all
faas-cli deploy -f my-project.yml
```

## Worker Service

The worker is a standard ASP.NET app using [Hangfire](https://www.hangfire.io/) for background job scheduling. It exposes a minimal HTTP endpoint for OpenFaaS health checks, and runs recurring jobs via Hangfire.

The template ships with `Hangfire.InMemory` for zero-config local development. For production, swap in a persistent storage backend (e.g. `Hangfire.SqlServer`, `Hangfire.Redis`).

### Configuration

Job intervals are controlled via environment variables:

```yaml
environment:
    sample-worker-interval-sec: "30"
```

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
faas-cli build -f my-project.yml --filter my-project-api --shrinkwrap
docker build --secret id=nuget.config,src=~/.nuget/NuGet/NuGet.Config build/my-project-api
```