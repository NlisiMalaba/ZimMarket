# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Layer cache: restore dependencies first
COPY ZimMarket.sln ./
COPY src/ZimMarket.Domain/ZimMarket.Domain.csproj src/ZimMarket.Domain/
COPY src/ZimMarket.Shared/ZimMarket.Shared.csproj src/ZimMarket.Shared/
COPY src/ZimMarket.Application/ZimMarket.Application.csproj src/ZimMarket.Application/
COPY src/ZimMarket.Infrastructure/ZimMarket.Infrastructure.csproj src/ZimMarket.Infrastructure/
COPY src/ZimMarket.API/ZimMarket.API.csproj src/ZimMarket.API/
RUN dotnet restore src/ZimMarket.API/ZimMarket.API.csproj

COPY src/ src/
RUN dotnet publish src/ZimMarket.API/ZimMarket.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
# Liveness (process up). Use GET /health/ready in orchestrators for DB + Redis dependency checks.
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "ZimMarket.API.dll"]
