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

   Copy `.env.example` to `.env` and fill in secrets (for example: `cp .env.example .env` on Git Bash/macOS/Linux, or `Copy-Item .env.example .env` in PowerShell). See comments in `.env.example` for Docker Compose and ASP.NET settings.

   Never commit `.env`; it is listed in `.gitignore`.

3. **Build**

   ```bash
   dotnet restore ZimMarket.sln
   dotnet build ZimMarket.sln
   ```

   For local file uploads without Azure Blob Storage, run the API with:

   ```bash
   Storage__Provider=Local
   LocalFileStorage__PublicBaseUrl=http://localhost:8080
   LocalFileStorage__RootPath=storage
   LocalFileStorage__SigningKey=<local-development-secret>
   ```

   **Docker:** create a root `.env` from `.env.example`, then `docker compose up --build` from the repo root. The API image is built from `src/ZimMarket.API/Dockerfile`. Smoke test: `GET http://localhost:${API_PORT:-8080}/health` should return **200**.

## Seed test users

For quick local API testing, seed one account per role (customer, seller, driver, admin, super admin context) with:

```powershell
./scripts/seed-test-users.ps1 -SuperAdminPassword "<your-super-admin-password>"
```

Optional flags:

- `-BaseUrl` (default: `http://localhost:5256`)
- `-SuperAdminEmail` (default: `superadmin@zimmarket.local`)

The script is idempotent for repeated runs and will reuse users that already exist.

If you prefer migration-based seeding, set:

- `ZIMMARKET_SEED_TEST_USERS=true`
- optional `ZIMMARKET_TEST_USERS_PASSWORD` (default: `TestPass123!`)

then run:

```bash
dotnet ef database update --project src/ZimMarket.Infrastructure --startup-project src/ZimMarket.API
```

This applies migration `20260504190300_SeedRoleTestUsers` and inserts one test account per role.

## Contributing

Follow the implementation order in `planning/TASKS.md` and architectural rules in `planning/DESIGN.md` (SOLID, Result pattern, layer boundaries).

## Load testing

Run the product search load test (Module 16.2) with `k6`:

```bash
k6 run tests/ZimMarket.Integration.Tests/product-search-load.k6.js
```

Optional environment variables:

- `BASE_URL` (default: `http://localhost:5000`)
- `SEARCH_TERM` (default: `integration`)
- `PAGE_SIZE` (default: `20`)

The script runs `200` concurrent users for `60` seconds and fails if `p95` latency is `>= 500ms` or error rate is `>= 1%`.

## Licence

Specify your licence here when the project defines one.
