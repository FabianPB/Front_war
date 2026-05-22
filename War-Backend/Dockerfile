# ═══════════════════════════════════════════════════════════════════
# WAR Online Demo — Multi-stage Dockerfile
# ═══════════════════════════════════════════════════════════════════

# ── Build stage ──
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (better layer caching)
COPY WARBackend.sln ./
COPY War.Core/War.Core.csproj War.Core/
COPY War.Infrastructure/War.Infrastructure.csproj War.Infrastructure/
COPY War.Api/War.Api.csproj War.Api/

# Restore dependencies
RUN dotnet restore WARBackend.sln

# Copy everything else and build
COPY War.Core/ War.Core/
COPY War.Infrastructure/ War.Infrastructure/
COPY War.Api/ War.Api/

RUN dotnet publish War.Api/War.Api.csproj -c Release -o /app --no-restore

# ── Runtime stage ──
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published output
COPY --from=build /app .

# Railway/Render/Fly.io provide PORT env variable
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "War.Api.dll"]
