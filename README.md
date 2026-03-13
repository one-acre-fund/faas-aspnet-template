# OpenFaaS ASPNET Functions (Multi-Project Support)

This project offers an advanced OpenFaaS template for ASP.NET 9. It is designed to cleanly support building **multiple backend functions and workers** that share code (like common libraries or domain models) within a single unified `.sln` solution.

> **Note:** The current active branch for .NET 9 multi-project support is `net9-multi`.

## Installing and Referencing the Template

Rather than downloading the template once via `faas-cli template pull`, you should reference it directly in your project's `.yml` files.

At the bottom of each of your OpenFaaS YML files, add:

```yaml
configuration:
    templates:
        - name: aspnet
          source: https://github.com/one-acre-fund/faas-aspnet-template#net9-multi
```

## Structure Overview

This template elegantly solves the "shared library" problem in OpenFaaS by operating at the **project root**. 
Unlike traditional OpenFaaS templates where 1 directory = 1 function, this template allows one shared context for the entire application.

```
my-project/
├── shared/
│   └── src/Shared.csproj
├── api/
│   └── src/api.csproj             ← References Shared.csproj
├── worker/
│   └── src/worker.csproj          ← References Shared.csproj
│
├── my-project-api.yml             ← handler: .
├── my-project-worker.yml          ← handler: .
│
├── .dockerignore                  ← Excludes bin/ and obj/
└── my-project.sln                 ← Restores all projects at once
```

## How to use it

### 1. Place YMLs at the root (`handler: .`)
Because all source code must be available in the Docker build context to resolve inter-project references, your YML files must live at the repository root and set `handler: .`

### 2. Set the `PUBLISH_PROJECT`
The template uses a single shared `Dockerfile`. When you build an image, you tell the Dockerfile which specific project to compile by passing it as a `build_arg`:

```yaml
version: 1.0
provider:
    name: openfaas
    gateway: http://127.0.0.1:8080
functions:
    my-project-api:
        lang: aspnet
        handler: .                         # Must be current directory
        image: acr/my-project-api:latest
        build_args:
            PUBLISH_PROJECT: "api/src/api.csproj"   # The exact file to publish
```

### 3. Dynamic Startup
You can name your `.csproj` files whatever you want. The Dockerfile extracts the project name from your `PUBLISH_PROJECT` argument and generates a dynamic startup script internally. 

There is **no need** to add `<AssemblyName>function</AssemblyName>` to your project files. `faas-cli push` and `faas-cli deploy` work out of the box.

## BuildKit Requirement

This template relies on the `COPY --parents` feature to preserve directory structures during the build. This requires Docker BuildKit.

When running a `build` or `up` command, you must explicitly enable BuildKit:

```bash
DOCKER_BUILDKIT=1 faas-cli build -f my-project-api.yml
```

*(Alternatively, export `DOCKER_BUILDKIT=1` in your `~/.bashrc` or `~/.zshrc`).*

## Running Unit Tests in CI
The Dockerfile includes a built-in safety gate. If it detects a directory named `function/tests/unit`, it will automatically restore and run `dotnet test` against it during the image build.

If a test fails, the image build fails. This guarantees broken code is never pushed to your registry.

You can bypass this locally by adding a `build_arg` to your YML:
```yaml
        build_args:
            RUN_TESTS: "false"
```
