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
- Solution and source projects will live at the repository root as they are added (see Module 0 in `planning/TASKS.md`)

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (when the solution is present)
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

3. **Build and run (once the solution exists)**

   ```bash
   dotnet restore
   dotnet build
   ```

   Docker-based local run will be documented when `docker-compose.yml` and the API project are in place (`docker compose up`, health check at `/health`).

## Contributing

Follow the implementation order in `planning/TASKS.md` and architectural rules in `planning/DESIGN.md` (SOLID, Result pattern, layer boundaries).

## Licence

Specify your licence here when the project defines one.
