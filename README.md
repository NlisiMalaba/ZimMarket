# ZimMarket

**Zimbabwe Marketplace** — MVP backend and supporting tooling for a multi-sided marketplace (customers, sellers, drivers, admins) with orders, payments in USD/ZWL, KYC, logistics, and real-time updates.

## Stack (target)

| Area | Technology |
|------|------------|
| API | ASP.NET Core 10 (Clean Architecture) |
| Data | PostgreSQL 16, EF Core |
| Cache / sessions | Redis 7 |
| Background jobs | Hangfire |
| Real-time | SignalR |
| Storage / integrations | Azure Blob, Twilio, SendGrid, Firebase (FCM), Paynow |

Client apps (React Native / Expo, Next.js admin) are planned alongside this repository.

## Repository layout

- `planning/` — task list and architecture reference (`TASKS.md`, `DESIGN.md`)
- `ZimMarket.sln` — solution file at the repository root
- `src/` — application projects (`ZimMarket.Domain`, `ZimMarket.Application`, `ZimMarket.Infrastructure`, `ZimMarket.API`, `ZimMarket.Shared`)
- `tests/` — test projects (xUnit)

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Postgres, Redis, API via Compose — added in later tasks)
- Git

## Local setup

1. **Clone the repository**

   ```bash
   git clone <your-remote-url> ZimMarket
   cd ZimMarket
   ```

2. **Environment variables**

   After `.env.example` is added (Module 0), copy it to `.env` and fill in secrets (for example: `cp .env.example .env` on Git Bash/macOS/Linux, or `Copy-Item .env.example .env` in PowerShell).

   Never commit `.env`; it is listed in `.gitignore`.

3. **Build**

   ```bash
   dotnet restore ZimMarket.sln
   dotnet build ZimMarket.sln
   ```

   **Docker:** create a root `.env` (see `.env.example` when present), then `docker compose up --build` from the repo root. The API image is built from `src/ZimMarket.API/Dockerfile`. Smoke test: `GET http://localhost:${API_PORT:-8080}/health` should return **200**.

## Contributing

Follow the implementation order in `planning/TASKS.md` and architectural rules in `planning/DESIGN.md` (SOLID, Result pattern, layer boundaries).

## Licence

Specify your licence here when the project defines one.
