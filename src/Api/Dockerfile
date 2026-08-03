# Multi-stage build. Api is currently the only deployable (Adapters.*/Infrastructure are class
# libraries it hosts, not separate processes) - see ADR-012 for why this is one Dockerfile, not
# "one per service" as the Implementation Plan's literal phrasing suggested.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Domain/HekCoreApi.Domain.csproj src/Domain/
COPY src/Contracts/HekCoreApi.Contracts.csproj src/Contracts/
COPY src/Application/HekCoreApi.Application.csproj src/Application/
COPY src/Infrastructure/HekCoreApi.Infrastructure.csproj src/Infrastructure/
COPY src/Adapters.Hiso/HekCoreApi.Adapters.Hiso.csproj src/Adapters.Hiso/
COPY src/Adapters.Karo/HekCoreApi.Adapters.Karo.csproj src/Adapters.Karo/
COPY src/Adapters.Erms/HekCoreApi.Adapters.Erms.csproj src/Adapters.Erms/
COPY src/Api/HekCoreApi.Api.csproj src/Api/
RUN dotnet restore src/Api/HekCoreApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/HekCoreApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# .NET 10's aspnet base image moved from Debian to Ubuntu - Debian's addgroup/adduser wrapper
# scripts aren't present, so use shadow-utils' groupadd/useradd directly instead. uid/gid bumped
# 1000 -> 1001: Ubuntu's base image already reserves 1000 for its own default "ubuntu" user/group.
RUN groupadd --system --gid 1001 hek && useradd --system --uid 1001 --gid hek hek
COPY --from=build /app .
# Serilog's File sink writes to logs/ (relative, resolves to /app/logs) - create it and hand
# ownership to the non-root "hek" user before switching, so the mounted log volume (docker-compose.yml)
# is writable instead of silently failing (Serilog swallows file-sink errors by default).
RUN mkdir -p /app/logs && chown -R hek:hek /app/logs
USER hek
EXPOSE 8080
ENTRYPOINT ["dotnet", "HekCoreApi.Api.dll"]
