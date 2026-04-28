# syntax=docker/dockerfile:1.7-labs
FROM ghcr.io/openfaas/of-watchdog:0.9.6 as watchdog

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 as builder
ARG AZURE_DEVOPS_FEED_USERNAME
ARG AZURE_DEVOPS_FEED_ACCESSTOKEN
ARG BUILDPLATFORM
ARG TARGETARCH

# The only arg that varies per sub-project.
# Override in your .yml:  build_args: { PUBLISH_PROJECT: "src/api/function.csproj" }
ARG PUBLISH_PROJECT="src/function.csproj"

RUN apt update && apt install libxml2-utils -y

# suppress data collection
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# ── Restore phase (NuGet cache layer) ──
# COPY --parents preserves directory structure from glob matches (BuildKit).
# .dockerignore excludes bin/, obj/, build/ so globs stay clean.
WORKDIR /source/faas/function
COPY function/*.slnx .
COPY --parents function/./**/*.csproj ./ 

# list files for debugging
RUN ls -R

# restore packages
COPY add-nuget-config.sh /tmp/
RUN chmod u+x /tmp/add-nuget-config.sh
RUN --mount=type=secret,id=nuget.config /tmp/add-nuget-config.sh nuget.config \
    dotnet restore -a $TARGETARCH

# ── Build phase ──
WORKDIR /source/faas
COPY . .

# Publish only the target sub-project
RUN dotnet publish \
    -a $TARGETARCH \
    -c release \
    --no-self-contained \
    -p:PublishReadyToRun=true \
    -o published \
    "function/$PUBLISH_PROJECT"

# ── Runner ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0

COPY --from=watchdog /fwatchdog /usr/bin/

WORKDIR /home/app/
COPY --from=builder /source/faas/published .

# Extract the DLL name from PUBLISH_PROJECT (e.g., "src/api/function.csproj" -> "function.dll")
ARG PUBLISH_PROJECT
RUN DLL_NAME=$(basename $PUBLISH_PROJECT .csproj).dll && \
    echo "#!/bin/sh\ndotnet ./$DLL_NAME" > function && \
    chmod +x function

RUN chown app:app -R /home/app

USER app

ENV fprocess="./function"
ENV cgi_headers="true"
ENV mode="http"
ENV upstream_url="http://localhost:9000"
ENV ASPNETCORE_URLS=http://+:9000

ENV exec_timeout="120s"
ENV write_timeout="125s"
ENV read_timeout="125s"

ENV prefix_logs="false"

EXPOSE 8080

HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1

CMD ["fwatchdog"]
