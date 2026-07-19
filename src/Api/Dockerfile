# Multi-stage build. Api is currently the only deployable (Adapters.*/Infrastructure are class
# libraries it hosts, not separate processes) - see ADR-012 for why this is one Dockerfile, not
# "one per service" as the Implementation Plan's literal phrasing suggested.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN addgroup --system --gid 1000 hek && adduser --system --uid 1000 --ingroup hek hek
COPY --from=build /app .
USER hek
EXPOSE 8080
ENTRYPOINT ["dotnet", "HekCoreApi.Api.dll"]
