# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build — SDK image has shell + tooling. Never reaches production.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Cache-friendly: project files first so restore layer is reused when source changes
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/Web.Api/Web.Api.csproj", "src/Web.Api/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/SharedKernel/SharedKernel.csproj", "src/SharedKernel/"]

RUN dotnet restore "./src/Web.Api/Web.Api.csproj"

COPY . .

WORKDIR "/src/src/Web.Api"

RUN dotnet build "./Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Publish — produce framework-dependent output, no AppHost wrapper
# ──────────────────────────────────────────────────────────────────────────────
FROM builder AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ──────────────────────────────────────────────────────────────────────────────
# Stage 3: Setup — Alpine provides non-root user entries
# dhi.io/dotnet is hardened (no shell in runtime).
# Use stock Alpine to prepare user entries, then copy them over.
# ──────────────────────────────────────────────────────────────────────────────
FROM alpine:3.23 AS setup

RUN addgroup -S nexus && adduser -S nexus -G nexus
# busybox provides wget for HEALTHCHECK in the shell-less runtime image
# Copied as /bin/wget: busybox detects the applet via argv[0]

# ──────────────────────────────────────────────────────────────────────────────
# Stage 4: Runtime — hardened, shell-less, COPY-only
# No RUN commands: avoids /bin/sh dependency on the hardened dotnet image.
# ──────────────────────────────────────────────────────────────────────────────
FROM dhi.io/dotnet:10-alpine3.23

# Build-time metadata — pass via CI build-args
ARG BUILD_DATE
ARG GIT_REVISION
ARG VERSION

LABEL org.opencontainers.image.title="Nexus API" \
      org.opencontainers.image.description=".NET 10 Clean Architecture API for Nexus Project" \
      org.opencontainers.image.source="https://github.com/Nexus-Project-app/Api" \
      org.opencontainers.image.documentation="https://github.com/Nexus-Project-app/Api" \
      org.opencontainers.image.vendor="Nexus Project" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${GIT_REVISION}" \
      org.opencontainers.image.version="${VERSION}"

# Bring in non-root user entries from Alpine setup stage
COPY --from=setup /etc/passwd /etc/passwd
COPY --from=setup /etc/group /etc/group

# busybox as wget for HEALTHCHECK — no shell needed, busybox detects applet by argv[0]
COPY --from=setup /bin/busybox /bin/wget

WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=publish --chown=nexus:nexus /app/publish .

USER nexus

HEALTHCHECK --interval=3600s --timeout=5s --start-period=15s --retries=3 \
    CMD ["/bin/wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "Web.Api.dll"]
