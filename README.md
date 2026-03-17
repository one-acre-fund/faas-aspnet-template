# OpenFaaS ASPNET Functions (Multi-Project Support)

This project offers an advanced OpenFaaS template for ASP.NET 9. It is designed to cleanly support building **multiple backend functions and workers** that share code (like common libraries or domain models) within a single unified `.sln` solution.

> **Note:** The current active branch for .NET 9 multi-project support is `net9-multi`.

## Installing and Referencing the Template

Rather than downloading the template once via `faas-cli template pull`, you should reference it directly in your project's `stack.yml`.

At the bottom of your `stack.yml`, add:

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
├── src/
│   ├── shared/shared.csproj           ← Shared library
│   ├── api/function.csproj            ← References shared.csproj
│   └── worker/worker.csproj           ← References shared.csproj
├── tests/
│   ├── unit/unit.csproj
│   └── integration/integration.csproj
├── stack.yml                          ← All functions defined here (handler: .)
├── .dockerignore                      ← Excludes bin/ and obj/
└── function.sln                       ← Restores all projects at once
```

## How to use it

### 1. Use a single `stack.yml` at the root (`handler: .`)
Because all source code must be available in the Docker build context to resolve inter-project references, your `stack.yml` must live at the repository root with each function setting `handler: .`

### 2. Set the `PUBLISH_PROJECT`
The template uses a single shared `Dockerfile`. Each function in `stack.yml` specifies which project to compile via the `PUBLISH_PROJECT` build arg:

```yaml
version: 1.0
provider:
    name: openfaas
    gateway: http://127.0.0.1:8080
functions:
    my-project-api:
        lang: aspnet
        handler: .
        image: acr/my-project-api:latest
        build_args:
            PUBLISH_PROJECT: "src/api/api.csproj"

    my-project-worker:
        lang: aspnet
        handler: .
        image: acr/my-project-worker:latest
        build_args:
            PUBLISH_PROJECT: "src/worker/worker.csproj"
```

### 3. Dynamic Startup
You can name your `.csproj` files whatever you want. The Dockerfile extracts the project name from your `PUBLISH_PROJECT` argument and generates a dynamic startup script internally. 

There is **no need** to add `<AssemblyName>function</AssemblyName>` to your project files. `faas-cli push` and `faas-cli deploy` work out of the box.

## BuildKit Requirement

This template relies on the `COPY --parents` feature to preserve directory structures during the build. This requires Docker BuildKit.

> **Note:** Docker 23.0+ ships with BuildKit enabled by default. If you're on an older version, set `DOCKER_BUILDKIT=1` as shown below.

```bash
DOCKER_BUILDKIT=1 faas-cli build -f stack.yml
```

*(Alternatively, export `DOCKER_BUILDKIT=1` in your `~/.bashrc` or `~/.zshrc`).*

